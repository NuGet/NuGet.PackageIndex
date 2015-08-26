// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Nuget.PackageIndex.Client;

namespace Nuget.PackageIndex.VisualStudio.Editor
{
    internal class NugetPackageTagger : ITagger<NugetPackageTag>
    {
        private readonly IAddPackageAnalyzerFactory _analyzerFactory;
        private readonly ITextView _textView;

        internal NugetPackageTagger(ITextView textView, IAddPackageAnalyzerFactory analyzerFactory)
        {
            _analyzerFactory = analyzerFactory;
            _textView = textView;
            // TODO Temporary
           // _textView.LayoutChanged += OnLayoutChanged;
        }

        IEnumerable<ITagSpan<NugetPackageTag>> ITagger<NugetPackageTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            // TODO temporary
            if (true)
            {
                return Enumerable.Empty<ITagSpan<NugetPackageTag>>();
            }

            var projectsMap = new Dictionary<string, IEnumerable<ProjectMetadata>>(StringComparer.OrdinalIgnoreCase);
            var resultTags = new List<ITagSpan<NugetPackageTag>>();
            foreach (var span in spans)
            {
                try
                {
                    if (span.Snapshot == null)
                    {
                        continue;
                    }

                    var document = span.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document == null)
                    {
                        continue;
                    }

                    IEnumerable<ProjectMetadata> projects = null;
                    if (projectsMap.Keys.Contains(document.FilePath))
                    {
                        projects = projectsMap[document.FilePath];
                    }
                    else
                    {
                        projects = ProjectMetadataProvider.Instance.GetProjects(document.FilePath);
                        projectsMap.Add(document.FilePath, projects);
                    }

                    if (projects == null || !projects.Any())
                    {
                        // project is unsupported
                        continue;
                    }

                    SemanticModel model = null;
                    ThreadHelper.JoinableTaskFactory.Run(async delegate
                    {
                        model = await document.GetSemanticModelAsync();
                    });

                    if (model == null)
                    {
                        continue;
                    }

                    var currentSpanLine = span.Start.GetContainingLine();
                    if (currentSpanLine == null || currentSpanLine.LineNumber < 0)
                    {
                        continue;
                    }

                    var analyzer = _analyzerFactory.GetAnalyzer(document.FilePath);
                    if (analyzer == null)
                    {
                        // language is unsupported
                        continue;
                    }

                    var diagnostics = model.GetDiagnostics();
                    if (diagnostics.Any(x =>
                    {
                        if (analyzer.SyntaxHelper.SupportedDiagnostics.Any(y => y.Equals(x.Id))
                            && x.Location.GetLineSpan().StartLinePosition.Line == currentSpanLine.LineNumber)
                        {
                            var suggestions = analyzer.GetSuggestions(x.Location.SourceTree.GetRoot().FindNode(x.Location.SourceSpan), projects);
                            if (suggestions != null && suggestions.Count() > 0)
                            {
                                return true;
                            }
                        }

                        return false;
                    }))
                    {
                        resultTags.Add(new TagSpan<NugetPackageTag>(new SnapshotSpan(span.Start, 0), new NugetPackageTag()));
                    }
                }
                catch (Exception e)
                {
                    Debug.Write(e.ToString());
                }
            }

            return resultTags;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            foreach (var span in e.NewOrReformattedSpans)
            {
                if (TagsChanged != null)
                {
                    TagsChanged(this, new SnapshotSpanEventArgs(span));
                }
            }
        }
    }
}