// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Nuget.PackageIndex.Abstractions
{
    internal class FileSystem : IFileSystem
    {
        public bool FileExists(string fullPath)
        {
            return File.Exists(fullPath);
        }

        public Stream FileOpenRead(string fullPath)
        {
            return File.OpenRead(fullPath);
        }

        public bool DirectoryExists(string fullPath)
        {
            return Directory.Exists(fullPath);
        }

        public string[] DirectoryGetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetFiles(path, searchPattern, searchOption);
        }

        public DateTime FileGetLastWriteTime(string fullPath)
        {
            return File.GetLastWriteTime(fullPath);
        }

        public string FileReadAllText(string fullPath)
        {
            return File.ReadAllText(fullPath);
        }

        public void FileWriteAllText(string fullPath, string content)
        {
            File.WriteAllText(fullPath, content);
        }

        public void FileDelete(string fullPath)
        {
            File.Delete(fullPath);
        }
    }
}
