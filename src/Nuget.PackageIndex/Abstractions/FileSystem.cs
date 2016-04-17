// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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

        public IEnumerable<string> DirectoryGetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateFiles(path, searchPattern, searchOption);
        }

        public IEnumerable<string> DirectoryGetFilesUpTo2Deep(string root, string pattern)
        {
            ConcurrentBag<string> located = new ConcurrentBag<string>();
            Parallel.ForEach(Directory.EnumerateDirectories(root), dir =>
            {
                bool filesFound = false;
                foreach (string file in Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly))
                {
                    filesFound = true;
                    located.Add(file);
                }

                if (!filesFound)
                {
                    foreach (string child in Directory.EnumerateDirectories(dir))
                    {
                        foreach (string file in Directory.EnumerateFiles(child, pattern))
                        {
                            located.Add(file);
                        }
                    }
                }
            });

            return located;
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
