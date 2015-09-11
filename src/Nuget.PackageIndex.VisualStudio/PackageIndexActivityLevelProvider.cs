// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Nuget.PackageIndex.VisualStudio
{
    internal enum ActivityLevel
    {
        On = 0,
        SuggestionsOnly = 1,
        Off = 2
    }

    /// <summary>
    /// Returns level of package index activity
    /// </summary>
    internal static class PackageIndexActivityLevelProvider
    {
        private const string ActivityLevelEnvironmentVariable = "DNX_PI_ACTIVITY_LEVEL";
        private static ActivityLevel? _activityLevel = null;
        public static ActivityLevel ActivityLevel
        {
            get
            {
                if (_activityLevel == null)
                {
                    var envVarValue = Environment.GetEnvironmentVariable(ActivityLevelEnvironmentVariable) ?? "0";
                    int intVal = 0;
                    Int32.TryParse(envVarValue, out intVal);

                    if (intVal < 0 || intVal > 2)
                    {
                        // if outside of supported range - error and set to default
                        intVal = 0;
                    }
                    _activityLevel = (ActivityLevel)intVal;
                }

                return _activityLevel.Value;
            }
        }
    }
}
