// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Core;

namespace Cake.Bakery.Arguments
{
    internal static class ArgumentParser
    {
        public static IDictionary<string, string> Parse(IEnumerable<string> arguments)
        {
            return arguments.Select(arg =>
            {
                var args = arg.UnQuote().TrimStart('-').Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                return Tuple.Create(args[0].ToLower(), args.Length < 2 ? string.Empty : args[1].UnQuote());
            }).ToDictionary(x => x.Item1, x => x.Item2);
        }
    }
}
