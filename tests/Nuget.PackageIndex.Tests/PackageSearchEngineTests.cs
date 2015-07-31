// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nuget.PackageIndex.Engine;

namespace Nuget.PackageIndex.Tests
{
    [TestClass()]
    public class PackageSearchEngineTests
    {
        [TestMethod()]
        public void PackageSearchEngineTests_IsReadOnly()
        {
            // Arrange, Act
            var engine = new PackageSearchEngine(null, null, null, readOnly: true);

            // Assert
            Assert.IsTrue(engine.IsReadonly);

            // Arrange, Act
            engine = new PackageSearchEngine(null, null, null, readOnly: false);

            // Assert
            Assert.IsFalse(engine.IsReadonly);
        }
    }
}