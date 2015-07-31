// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Nuget.PackageIndex.Models
{
    /// <summary>
    /// Type metadata exposed publicly 
    /// </summary>
    public class ModelBase
    { 
         public List<string> TargetFrameworks { get; internal set; }

        public ModelBase()
        {
            TargetFrameworks = new List<string>();
        }

        public void MergeTargetFrameworks(IEnumerable<string> newTargetFrameworks)
        {
            if (newTargetFrameworks == null)
            {
                return;
            }

            foreach(var newFx in newTargetFrameworks)
            {
                if (TargetFrameworks.All(x => !x.Equals(newFx, StringComparison.OrdinalIgnoreCase)))
                {
                    TargetFrameworks.Add(newFx);
                }
            }
        }
    }
}
