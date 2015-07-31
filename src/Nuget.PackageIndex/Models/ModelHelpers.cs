// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography;
using System.Text;

namespace Nuget.PackageIndex.Models
{
    /// <summary>
    /// Helper methods used by models
    /// </summary>
    internal static class ModelHelpers
    {
        public static string GetMD5Hash(string entity)
        {
            if (string.IsNullOrEmpty(entity))
            {
                return null;
            }

            // convert entity to byte array
            var entityContents = entity;
            var byteContainer = new byte[entityContents.Length * 2];
            Encoding.UTF8.GetEncoder().GetBytes(entityContents.ToCharArray(), 0, entityContents.Length, byteContainer, 0, true);

            // generate MD5 hash for byte array
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(byteContainer);

            // convert hash to hex string
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < result.Length; i++)
            {
                stringBuilder.Append(result[i].ToString("X2"));
            }

            return stringBuilder.ToString();
        }
    }
}
