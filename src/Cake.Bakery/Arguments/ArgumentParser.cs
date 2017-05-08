using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Core;

namespace Cake.Bakery.Arguments
{
    internal static class ArgumentParser
    {
        public static IReadOnlyDictionary<string, string> Parse(IEnumerable<string> arguments)
        {
            return arguments.Select(arg =>
            {
                var args = arg.UnQuote().TrimStart('-').Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                return Tuple.Create(args[0].ToLower(), args.Length < 2 ? string.Empty : args[1].UnQuote());
            }).ToDictionary(x => x.Item1, x => x.Item2);
        }
    }
}
