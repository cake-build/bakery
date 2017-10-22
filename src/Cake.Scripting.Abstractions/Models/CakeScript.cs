// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Cake.Scripting.Abstractions.Models
{
    public sealed class CakeScript
    {
        private HashSet<string> _references;
        private HashSet<string> _usings;
        private ScriptHost _host;

        public static CakeScript Empty => new CakeScript();

        public ScriptHost Host => _host ?? (_host = new ScriptHost());

        public string Source { get; set; }

        public ISet<string> References => _references ?? (_references = new HashSet<string>());

        public ISet<string> Usings => _usings ?? (_usings = new HashSet<string>());
    }
}
