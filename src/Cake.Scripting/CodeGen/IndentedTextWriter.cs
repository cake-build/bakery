// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

namespace Cake.Scripting.CodeGen
{
    internal sealed class IndentedTextWriter : TextWriter
    {
        private readonly TextWriter _writer;
        private int _level;
        private bool _performIndentation;

        public override Encoding Encoding => _writer.Encoding;

        private sealed class Scope : IDisposable
        {
            private readonly Action _disposeAction;

            public Scope(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                _disposeAction();
            }
        }

        public IndentedTextWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public void IncreaseLevel()
        {
            _level += 1;
        }

        public void DecreaseLevel()
        {
            _level -= 1;
        }

        public override void Write(char ch)
        {
            if (_performIndentation)
            {
                _performIndentation = false;
                for (var index = 0; index < _level; ++index)
                {
                    _writer.Write("    ");
                }
            }

            _writer.Write(ch);

            if (ch == '\n')
            {
                _performIndentation = true;
            }
        }

        public IDisposable BeginScope()
        {
            WriteLine();
            IncreaseLevel();
            return new Scope(() =>
            {
                DecreaseLevel();
                WriteLine();
            });
        }
    }
}
