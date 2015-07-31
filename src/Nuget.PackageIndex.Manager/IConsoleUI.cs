// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Nuget.PackageIndex.Manager
{   
    internal interface IConsoleUI
    {
        void WriteIntro(string message);
        void WriteImportantLine(string message);
        void WriteHighlitedLine(string message);
        void WriteNormalLine(string message);
    }
}
