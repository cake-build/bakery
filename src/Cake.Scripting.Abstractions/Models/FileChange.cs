using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cake.Scripting.Abstractions.Models
{
    public sealed class FileChange
    {
        private Collection<LineChange> _lineChanges;

        public bool FromDisk { get; set; }

        public string Buffer { get; set; }

        public string FileName { get; set; }

        public ICollection<LineChange> LineChanges => _lineChanges ?? (_lineChanges = new Collection<LineChange>());

        public static FileChange Empty => new FileChange();
    }
}
