namespace rg_gui
{
    public class TermResult
    {
        public TermResult(int start, int end, int termIndex)
        {
            Start = start;
            End = end;
            TermIndex = termIndex;
        }

        public int Start { get; set; }
        public int End { get; set; }
        public int TermIndex { get; }
    }
}