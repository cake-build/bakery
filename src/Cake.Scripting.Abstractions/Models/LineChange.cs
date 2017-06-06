// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Cake.Scripting.Abstractions.Models
{
    public sealed class LineChange : IEquatable<LineChange>
    {
        public string NewText { get; set; }

        public int StartLine { get; set; }

        public int StartColumn { get; set; }

        public int EndLine { get; set; }

        public int EndColumn { get; set; }

        public bool Equals(LineChange other)
        {
            if (other == null)
            {
                return false;
            }

            return NewText == other.NewText
                   && StartLine == other.StartLine
                   && StartColumn == other.StartColumn
                   && EndLine == other.EndLine
                   && EndColumn == other.EndColumn;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LineChange);
        }

        public override int GetHashCode()
        {
            return unchecked (NewText.GetHashCode()
                   * (23 + StartLine)
                   * (29 + StartColumn)
                   * (31 + EndLine)
                   * (37 + EndColumn));
        }
    }
}
