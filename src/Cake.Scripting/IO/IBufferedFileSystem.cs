// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Cake.Core.IO;
using Cake.Scripting.Abstractions.Models;

namespace Cake.Scripting.IO
{
    public interface IBufferedFileSystem : IFileSystem
    {
        void UpdateFileBuffer(FilePath path, string buffer);

        void UpdateFileBuffer(FilePath path, ICollection<LineChange> lineChanges);

        void RemoveFileFromBuffer(FilePath path);
    }
}
