// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using IFileSystem = Nuget.PackageIndex.Abstractions.IFileSystem;

namespace Nuget.PackageIndex.Tests
{
    [TestClass]
    public class NupkgLocalPackageLoaderTests
    {
        [TestMethod]
        public void NupkgLocalPackageLoader_GetPackages_NoSourceDirsExist()
        {
            // Arrange
            var indexedPackages = new HashSet<string>();
            var sourceDirectories = new List<string>();
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

            // Act           
            var loader = new NupkgLocalPackageLoader(mockFileSystem.Object, null);
            var returnedPackages = loader.GetPackages(sourceDirectories,
                                                      indexedPackages,
                                                      newOnly: false,
                                                      lastIndexModifiedTime: DateTime.Now,
                                                      cancellationToken: CancellationToken.None);
            // Assert
            Assert.IsFalse(returnedPackages.Any());
            mockFileSystem.VerifyAll();

            // Arrange 
            sourceDirectories = new List<string> {
                @"d:\somefolder\SomeSubFolder",
                @"c:\Program Files\SomeSubFolder"
            };
            foreach (var dir in sourceDirectories)
            {
                mockFileSystem.Setup(x => x.DirectoryExists(dir)).Returns(false);
            }

            // Act
            returnedPackages = loader.GetPackages(sourceDirectories,
                                                      indexedPackages,
                                                      newOnly: false,
                                                      lastIndexModifiedTime: DateTime.Now,
                                                      cancellationToken: CancellationToken.None);

            // Assert
            Assert.IsFalse(returnedPackages.Any());
            mockFileSystem.VerifyAll();
        }

        [TestMethod]
        public void NupkgLocalPackageLoader_GetPackages_NewOnly()
        {
            // Arrange
            var indexedPackages = new HashSet<string>();
            var sourceDirectories = new Dictionary<string, string[]> {
                { @"d:\somefolder\SomeSubFolder", new [] { @"d:\somefolder\SomeSubFolder\dpackage1.nuspec", @"d:\somefolder\SomeSubFolder\dpackage2.nuspec"} },
                { @"c:\Program Files\SomeSubFolder", new [] { @"c:\Program Files\SomeSubFolder\cpackage1.nuspec", @"c:\Program Files\SomeSubFolder\cpackage2.nuspec" } }
            };

            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            foreach (var dirKvp in sourceDirectories)
            {
                mockFileSystem.Setup(x => x.DirectoryExists(dirKvp.Key)).Returns(true);
                mockFileSystem.Setup(x => x.DirectoryGetFiles(dirKvp.Key, "*.nuspec", SearchOption.AllDirectories)).Returns(dirKvp.Value);
            }

            mockFileSystem.Setup(x => x.FileGetLastWriteTime(@"d:\somefolder\SomeSubFolder\dpackage1.nuspec")).Returns(DateTime.Now.AddDays(1));
            mockFileSystem.Setup(x => x.FileGetLastWriteTime(@"d:\somefolder\SomeSubFolder\dpackage2.nuspec")).Returns(DateTime.Now.AddDays(-1));
            mockFileSystem.Setup(x => x.FileGetLastWriteTime(@"c:\Program Files\SomeSubFolder\cpackage1.nuspec")).Returns(DateTime.Now.AddDays(-1));
            mockFileSystem.Setup(x => x.FileGetLastWriteTime(@"c:\Program Files\SomeSubFolder\cpackage2.nuspec")).Returns(DateTime.Now.AddDays(1));

            // Act           
            var loader = new NupkgLocalPackageLoader(mockFileSystem.Object, null);
            var returnedPackages = loader.GetPackages(sourceDirectories.Keys,
                                                      indexedPackages,
                                                      newOnly: true,
                                                      lastIndexModifiedTime: DateTime.Now,
                                                      cancellationToken: CancellationToken.None).ToList();
            // Assert
            Assert.AreEqual(2, returnedPackages.Count());
            Assert.AreEqual(@"d:\somefolder\SomeSubFolder\dpackage1.nuspec", returnedPackages[0]);
            Assert.AreEqual(@"c:\Program Files\SomeSubFolder\cpackage2.nuspec", returnedPackages[1]);
            mockFileSystem.VerifyAll();
        }

        [TestMethod]
        public void NupkgLocalPackageLoader_GetPackages_AlreadyIndexed()
        {
            // Arrange
            var indexedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            indexedPackages.Add(@"c:\Program Files\SomeSubFolder\cpackage1.nuspec");
            var sourceDirectories = new Dictionary<string, string[]> {
                { @"d:\somefolder\SomeSubFolder", new [] { @"d:\somefolder\SomeSubFolder\dpackage1.nuspec", @"d:\somefolder\SomeSubFolder\dpackage2.nuspec"} },
                { @"c:\Program Files\SomeSubFolder", new [] { @"c:\Program Files\SomeSubFolder\cpackage1.nuspec", @"c:\Program Files\SomeSubFolder\cpackage2.nuspec" } }
            };

            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            foreach (var dirKvp in sourceDirectories)
            {
                mockFileSystem.Setup(x => x.DirectoryExists(dirKvp.Key)).Returns(true);
                mockFileSystem.Setup(x => x.DirectoryGetFiles(dirKvp.Key, "*.nuspec", SearchOption.AllDirectories)).Returns(dirKvp.Value);
            }


            // Act           
            var loader = new NupkgLocalPackageLoader(mockFileSystem.Object, null);
            var returnedPackages = loader.GetPackages(sourceDirectories.Keys,
                                                      indexedPackages,
                                                      newOnly: false,
                                                      lastIndexModifiedTime: DateTime.Now,
                                                      cancellationToken: CancellationToken.None);
            // Assert
            Assert.AreEqual(3, returnedPackages.Count());
            mockFileSystem.VerifyAll();
        }
    }
}