// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;

namespace Nuget.PackageIndex.NugetHelpers
{
    internal class SelectionCriteriaBuilder
    {
        private IDictionary<string, ContentPropertyDefinition> propertyDefinitions;

        public SelectionCriteriaBuilder(IDictionary<string, ContentPropertyDefinition> propertyDefinitions)
        {
            this.propertyDefinitions = propertyDefinitions;
        }

        public virtual SelectionCriteria Criteria { get; } = new SelectionCriteria();

        internal virtual SelectionCriteriaEntryBuilder Add
        {
            get
            {
                var entry = new SelectionCriteriaEntry();
                Criteria.Entries.Add(entry);
                return new SelectionCriteriaEntryBuilder(this, entry);
            }
        }

        internal class SelectionCriteriaEntryBuilder : SelectionCriteriaBuilder
        {
            public SelectionCriteriaEntry Entry { get; }
            public SelectionCriteriaBuilder Builder { get; }

            public SelectionCriteriaEntryBuilder(SelectionCriteriaBuilder builder, SelectionCriteriaEntry entry) : base(builder.propertyDefinitions)
            {
                Builder = builder;
                Entry = entry;
            }
            public SelectionCriteriaEntryBuilder this[string key, string value]
            {
                get
                {
                    ContentPropertyDefinition propertyDefinition;
                    if (!propertyDefinitions.TryGetValue(key, out propertyDefinition))
                    {
                        throw new Exception("Undefined property used for criteria");
                    }
                    if (value == null)
                    {
                        Entry.Properties[key] = null;
                    }
                    else
                    {
                        object valueLookup;
                        if (propertyDefinition.TryLookup(value, out valueLookup))
                        {
                            Entry.Properties[key] = valueLookup;
                        }
                        else
                        {
                            throw new Exception("Undefined value used for criteria");
                        }
                    }
                    return this;
                }
            }
            public SelectionCriteriaEntryBuilder this[string key, object value]
            {
                get
                {
                    ContentPropertyDefinition propertyDefinition;
                    if (!propertyDefinitions.TryGetValue(key, out propertyDefinition))
                    {
                        throw new Exception("Undefined property used for criteria");
                    }
                    Entry.Properties[key] = value;
                    return this;
                }
            }
            internal override SelectionCriteriaEntryBuilder Add
            {
                get
                {
                    return Builder.Add;
                }
            }
            public override SelectionCriteria Criteria
            {
                get
                {
                    return Builder.Criteria;
                }
            }
        }
    }
}