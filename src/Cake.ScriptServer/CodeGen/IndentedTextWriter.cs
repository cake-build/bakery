using System;
using System.IO;
using System.Text;

namespace Cake.ScriptServer.CodeGen
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
                for (var ix = 0; ix < _level; ++ix)
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