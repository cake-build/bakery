using System.IO;

namespace Cake.ScriptServer.IO
{
    internal interface IConsole
    {
        TextWriter StdOut { get; }

        TextReader StdIn { get; }

        TextWriter StdErr { get; }
    }
}
