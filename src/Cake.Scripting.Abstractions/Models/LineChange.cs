namespace Cake.Scripting.Abstractions.Models
{
    public sealed class LineChange
    {
        public string NewText { get; set; }

        public int StartLine { get; set; }

        public int StartColumn { get; set; }

        public int EndLine { get; set; }

        public int EndColumn { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as LineChange;
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

        public override int GetHashCode()
        {
            return NewText.GetHashCode()
                   * (23 + StartLine)
                   * (29 + StartColumn)
                   * (31 + EndLine)
                   * (37 + EndColumn);
        }
    }
}