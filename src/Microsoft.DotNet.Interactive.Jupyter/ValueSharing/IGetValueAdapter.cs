﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

internal interface IGetValueAdapter
{
    Task<IValueAdapter> GetValueAdapter(KernelInfo kernelInfo);
}