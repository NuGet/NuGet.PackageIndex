// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Nuget.PackageIndex.Abstractions
{
    public interface IFileSystem
    {
        bool FileExists(string fullPath);
        Stream FileOpenRead(string fullPath);
        bool DirectoryExists(string fullPath);
        string[] DirectoryGetFiles(string path, string searchPattern, SearchOption searchOption);
        DateTime FileGetLastWriteTime(string fullPath);
        string FileReadAllText(string fullPath);
        void FileWriteAllText(string fullPath, string content);
        void FileDelete(string fullPath);
    }
}
