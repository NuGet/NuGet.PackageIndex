// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuget.PackageIndex.Abstractions;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Represents settings.json file located in the index directory. If file does not
    /// exist, creates a new settings.json with default settings. When user modifies, settings.json
    /// changes are in effect after VS is re-opened.
    /// </summary>
    public class IndexSettings : IIndexSettings
    {
        private const string SettingJsonFileName = "packageIndexSettings.json";
        private const string DefaultJsonContent = @"{
  'version': '1.0.0.0',
  'includePackagePatterns': []
}
";
        /// <summary>
        /// These are prefixes for package names that are shipped under,
        /// %ProgramFiles(x86)%\Microsoft Web Tools\DNU. We do look for packages 
        /// at multiple local folders (sources), but index only currated list of 
        /// packages. If users want to add other packages to the index they could 
        /// update file %UserProfile%\AppData\Local\Microsoft\PackageIndex\settings.json
        /// and add a regex for their packages to "include" list.
        /// </summary>
        private readonly string[] _defaultIncludeRules = new []
        {
            @"system\..+",
            @"microsoft\..+",
            @"entityframework\..+",
            @"newtonsoft\..+"
        };

        private readonly IFileSystem _fileSystem;
        private string _settingsJsonPath;

        public IndexSettings(string indexDirectory)
            : this(indexDirectory, new FileSystem())
        {

        }

        internal IndexSettings(string indexDirectory, IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _settingsJsonPath = Path.Combine(indexDirectory, SettingJsonFileName);
        }

        private JObject InitializeSettingsJson()
        {
            JObject jObject = null;

            if (_fileSystem.FileExists(_settingsJsonPath))
            {
                jObject = Read(_settingsJsonPath);
            } 
            else
            {
                jObject = GenerateDefaultSettingsJson();
                Write(jObject);
            }

            return jObject;
        }

        private JObject GenerateDefaultSettingsJson()
        {
            var jObject = JObject.Parse(DefaultJsonContent);
            var includePackagePaterns = (JArray)jObject["includePackagePatterns"];

            foreach(var pattern in _defaultIncludeRules)
            {
                includePackagePaterns.Add(pattern);
            }

            return jObject;
        }

        private JObject Read(string filePath)
        {
            JObject result = null;

            try
            {
                var fileContent = _fileSystem.FileReadAllText(filePath);
                result = JObject.Parse(fileContent);
            }
            catch (Exception e)
            {
                Debug.Write(e.ToString());
            }

            return result;
        }

        private void Write(JObject json)
        {
            try
            {
                if (_fileSystem.FileExists(_settingsJsonPath))
                {
                    _fileSystem.FileDelete(_settingsJsonPath);
                }

                using (FileStream fs = File.Open(_settingsJsonPath, FileMode.OpenOrCreate))
                using (StreamWriter sw = new StreamWriter(fs))
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    jw.Formatting = Formatting.Indented;

                    json.WriteTo(jw);
                }
            }
            catch (Exception e)
            {
                Debug.Write(e.ToString());
            }
        }

        private IList<string> ReadJsonArray(string nodeName)
        {
            try
            {
                var settingsJson = InitializeSettingsJson();
                if (settingsJson != null)
                {
                    var array = settingsJson[nodeName] as JArray;
                    if (array != null)
                    {
                        return array.Select(x => x.ToString()).ToList();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Write(e.ToString());
            }

            return null;
        }

        private IList<string> GetIncludePackagePatterns()
        {
            IList<string> result = null;

            var includeRulesFromJsonFile = ReadJsonArray("includePackagePatterns");
            if (includeRulesFromJsonFile != null)
            {
                result = new List<string>(includeRulesFromJsonFile);
            }

            return result ?? new List<string>(_defaultIncludeRules);
        }

        private IList<string> _includePackagePatterns;
        public IList<string> IncludePackagePatterns
        {
            get
            {
                if (_includePackagePatterns == null)
                {
                    _includePackagePatterns = GetIncludePackagePatterns();
                }

                return _includePackagePatterns;
            }
        }
    }
}
