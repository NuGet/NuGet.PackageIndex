// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nuget.PackageIndex.Manager
{
    internal class OptionsSet
    {
        private readonly List<Option> _options;

        public OptionsSet()
        {
            _options = new List<Option>();
        }

        public void AddOption(string pattern, string message, Action<string> action)
        {
            _options.Add(new Option(pattern, message, action));
        }

        public void Parse(string[] args)
        {
            for (var index = 0; index < args.Length; index++)
            {
                Option option = null;
                var arg = args[index];
                string[] longOption = null;
                string[] shortOption = null;

                if (arg.StartsWith("--"))
                {
                    longOption = arg.Substring(2).Split(new[] { ':', '=' }, 2);
                }
                else if (arg.StartsWith("-"))
                {
                    shortOption = arg.Substring(1).Split(new[] { ':', '=' }, 2);
                }

                string[] optionValues = null;
                if (longOption != null)
                {
                    option = _options.SingleOrDefault(opt => string.Equals(opt.LongName, longOption[0], StringComparison.Ordinal));
                    optionValues = longOption;
                }
                else if (shortOption != null)
                {
                    option = _options.SingleOrDefault(opt => string.Equals(opt.ShortName, shortOption[0], StringComparison.Ordinal));
                    optionValues = shortOption;
                }

                if (option == null)
                {
                    throw new Exception(string.Format(Resources.UnrecognizedArgument, arg));
                }

                if (optionValues.Length > 2)
                {
                    throw new Exception(string.Format(Resources.IncorrectArgumentFormat, arg));
                }

                if (optionValues.Length == 2 && option.IsFlag)
                {
                    throw new Exception(string.Format(Resources.ArgumentIsFlag, arg));
                }

                if (optionValues.Length == 1 && !option.IsFlag)
                {
                    throw new Exception(string.Format(Resources.ArgumentExpectedValue, arg));
                }

                if (optionValues.Length == 2)
                {
                    option.SetValue(optionValues[1]);
                }
                else
                {
                    option.SetValue(optionValues[0]);
                }

                if (option.IsHelpOption)
                {
                    // if help then stop processing options and quit
                    break;
                }
            }
        }
    
        public void PrintHelpMessage(TextWriter stream)
        {
            var longestPattern = _options.Max(x => x.Pattern.Length);
            if (longestPattern == 0)
            {
                return;
            }

            longestPattern = longestPattern + 4;
            foreach (var option in _options)
            {
                var text = option.Pattern.PadRight(longestPattern) + option.Message;
                stream.WriteLine(text);
            }

            stream.WriteLine(string.Empty);
        }

        private class Option
        {
            public string Pattern { get; private set; }
            public string ShortName { get; private set; }
            public string LongName { get; private set; }
            public bool IsFlag { get; private set; }
            public string Message { get; private set; }
            public Action<string> ProcessValueAction { get; private set; }

            public bool IsHelpOption { get; private set; }
            public Option(string pattern, string message, Action<string> processValueAction, bool isHelp = false)
            {
                Pattern = pattern == null ? null : pattern.Trim();
                Message = message;
                ProcessValueAction = processValueAction;
                IsHelpOption = isHelp;
                IsFlag = true;

                if (string.IsNullOrEmpty(Pattern))
                {
                    throw new Exception(Resources.String1ArgumentPatternCanNotBeEmpty);
                }

                var tempPattern = Pattern;
                if (tempPattern.EndsWith("="))
                {
                    tempPattern = tempPattern.TrimEnd('=');
                }

                foreach (var part in Pattern.Split(new[] { ' ', '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var tempPart = part;
                    if (tempPart.EndsWith("="))
                    {
                        IsFlag = false;
                        tempPart = tempPart.TrimEnd('=');
                    }
                    if (tempPart.StartsWith("--"))
                    {
                        LongName = tempPart.Substring(2);
                    }
                    else if (tempPart.StartsWith("-"))
                    {
                        ShortName = tempPart.Substring(1);
                    }                    
                    else
                    {
                        throw new Exception(string.Format(Resources.InvalidArgumentPattern, Pattern));
                    }

                }

                if (string.IsNullOrEmpty(LongName) && string.IsNullOrEmpty(ShortName))
                {
                    throw new Exception(string.Format(Resources.InvalidArgumentPattern, Pattern));
                }
            }

            public void SetValue(string val)
            {
                if (ProcessValueAction != null)
                {
                    ProcessValueAction(val);
                }
            }
        }
    }
}