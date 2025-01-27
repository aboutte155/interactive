// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

internal class PackageNameIsFullyQualifiedPath : IPackageFinder
{
    public async Task<T> Find<T>(PackageDescriptor descriptor)
        where T : class, IPackage
    {
        if (descriptor.IsPathSpecified)
        {
            var pkg = new Package2(descriptor.Name, new FileSystemDirectoryAccessor(new FileInfo(descriptor.Name).Directory));

            if (pkg is T t)
            {
                return t;
            }
        }

        return default;
    }
}