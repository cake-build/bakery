using System.IO;

namespace Cake.ScriptServer.IO
{
    internal class Console : IConsole
    {
        public TextWriter StdOut => System.Console.Out;
        public TextReader StdIn => System.Console.In;
        public TextWriter StdErr => System.Console.Error;
    }
}
