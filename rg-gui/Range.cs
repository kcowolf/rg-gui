using System.Collections.Generic;

namespace rg_gui
{
    public class Range
    {
        public Range(int start, int end)
        {
            Start = start;
            End = end;
        }

        public bool Overlaps(Range other)
        {
            return Start <= other.End && other.Start <= End;
        }

        public IEnumerable<Range> GetNonOverlappingRanges(Range other)
        {
            var results = new List<Range>();

            if (!Overlaps(other))
            {
                results.Add(new Range(Start, End));
            }
            else
            {
                if (Start < other.Start)
                {
                    results.Add(new Range(Start, other.Start - 1));
                }

                if (End > other.End)
                {
                    results.Add(new Range(other.End + 1, End));
                }
            }

            return results;
        }

        public int Start { get; }
        public int End { get; }
    }
}