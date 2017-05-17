using System.Collections.Generic;

namespace Cake.Scripting.Abstractions.Models
{
    public sealed class CakeScript
    {
        private HashSet<string> _references;
        private HashSet<string> _usings;

        public static CakeScript Empty = new CakeScript();

        public string Source { get; set; }

        public ISet<string> References => _references ?? (_references = new HashSet<string>());

        public ISet<string> Usings => _usings ?? (_usings = new HashSet<string>());
    }
}
