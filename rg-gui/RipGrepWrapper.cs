using CliWrap;
using CliWrap.EventStream;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace rg_gui
{
    public class RipGrepWrapper
    {
        public enum FileEncoding
        {
            Auto,
            GBK
        }

        public enum MaxFileSizeUnit
        {
            None,
            B,
            K,
            M,
            G
        }

        public class SearchParameters
        {
            public string StartPath { get; set; } = string.Empty;

            public string SearchString { get; set; } = string.Empty;

            public string IncludePatterns { get; set; } = string.Empty;

            public string ExcludePatterns { get; set; } = string.Empty;

            public bool IncludeHiddenFiles { get; set; } = true;

            public bool IgnoreCase { get; set; } = true;

            public bool Recursive { get; set; } = true;

            public bool RegularExpression { get; set; } = true;

            public FileEncoding Encoding { get; set; } = FileEncoding.Auto;

            public int MaxFileSize { get; set; }

            public MaxFileSizeUnit MaxFileSizeUnit { get; set; } = MaxFileSizeUnit.None;
        }

        public class TermResult
        {
            public TermResult(int termIndex, Range range)
            {
                TermIndex = termIndex;
                Range = range;
            }

            public int TermIndex { get; }

            public Range Range { get; }
        }

        public class LineResult
        {
            public LineResult(string lineContent)
            {
                LineContent = lineContent;
                TermResults = new();
            }

            public string LineContent { get; }
            public ConcurrentBag<TermResult> TermResults { get; }
        }

        public readonly ConcurrentBag<(string path, string filename, int termIndex)> FilesFound = new();
        public readonly ConcurrentDictionary<(string path, string filename, int lineNumber), LineResult> FileResults = new();

        public event EventHandler<(string path, string filename, int termIndex)>? FileFound;
        protected void RaiseFileFound(string path, string filename, int termIndex)
        {
            FileFound?.Invoke(this, (path, filename, termIndex));
        }

        private string m_ripGrepPath;

        public RipGrepWrapper(string ripGrepPath)
        {
            m_ripGrepPath = ripGrepPath;
        }

        public void Clear()
        {
            FilesFound.Clear();
            FileResults.Clear();
        }

        public async Task Search(SearchParameters searchParameters, CancellationToken cancellationToken, int termIndex)
        {
            const string fieldMatchSeparator = "\t";

            if (string.IsNullOrWhiteSpace(searchParameters.StartPath))
            {
                return;
            }

            var argsBuilder = new StringBuilder();
            argsBuilder.Append("-uu ");
            argsBuilder.Append("--no-heading ");
            argsBuilder.Append("--line-number ");
            argsBuilder.Append($"--field-match-separator=\"{fieldMatchSeparator}\" ");

            if (searchParameters.IgnoreCase)
            {
                argsBuilder.Append("-i ");
            }

            if (searchParameters.IncludeHiddenFiles)
            {
                argsBuilder.Append("--hidden ");
            }

            if (!searchParameters.Recursive)
            {
                argsBuilder.Append("--max-depth=1 ");
            }

            if (!searchParameters.RegularExpression)
            {
                argsBuilder.Append("--fixed-strings ");
            }

            if (!string.IsNullOrWhiteSpace(searchParameters.IncludePatterns))
            {
                argsBuilder.Append("--iglob={");
                argsBuilder.AppendJoin(",", GetSearchPatterns(searchParameters.IncludePatterns));
                argsBuilder.Append("} ");
            }

            if (searchParameters.ExcludePatterns.Any())
            {
                argsBuilder.Append("--iglob=!{");
                argsBuilder.AppendJoin(",", GetSearchPatterns(searchParameters.ExcludePatterns));
                argsBuilder.Append("} ");
            }

            argsBuilder.Append("--color always ");

            if (searchParameters.Encoding != FileEncoding.Auto)
            {
                argsBuilder.Append($"-E {EncodingTypes[searchParameters.Encoding]} ");
            }

            if (searchParameters.MaxFileSizeUnit != MaxFileSizeUnit.None)
            {
                argsBuilder.Append($"--max-filesize {searchParameters.MaxFileSize}{(searchParameters.MaxFileSizeUnit != MaxFileSizeUnit.B ? searchParameters.MaxFileSizeUnit : string.Empty)} ");
            }

            // Signal no more flags will be set.
            argsBuilder.Append("-- ");

            if (!string.IsNullOrWhiteSpace(searchParameters.SearchString))
            {
                argsBuilder.Append(searchParameters.SearchString);
                argsBuilder.Append(' ');
            }

            argsBuilder.Append($"\"{searchParameters.StartPath}\"");

            var cmd = Cli.Wrap(m_ripGrepPath)
                .WithArguments(argsBuilder.ToString())
                .WithValidation(CommandResultValidation.None);
            
            try
            {
                await foreach (var cmdEvent in cmd.ListenAsync(Encoding.UTF8, cancellationToken))
                {
                    switch (cmdEvent)
                    {
                        case StandardOutputCommandEvent stdOut:
                            {
                                var result = stdOut.Text.Split(fieldMatchSeparator, 3);

                                if (result.Length == 3 &&
                                    !string.IsNullOrWhiteSpace(result[0]) &&
                                    !string.IsNullOrWhiteSpace(result[1]) &&
                                    !string.IsNullOrWhiteSpace(result[2]) &&
                                    int.TryParse(RemoveAnsiColors(result[1]), out int lineNumber)
                                    )
                                {
                                    var fullPath = RemoveAnsiColors(result[0]);
                                    var path = Path.GetDirectoryName(fullPath);
                                    var filename = Path.GetFileName(fullPath);

                                    if (!string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(filename))
                                    {
                                        if (!FilesFound.Contains((path, filename, termIndex)))
                                        {
                                            FilesFound.Add((path, filename, termIndex));
                                            RaiseFileFound(path, filename, termIndex);
                                        }

                                        if (!FileResults.ContainsKey((path, filename, lineNumber)))
                                        {
                                            FileResults.GetOrAdd((path, filename, lineNumber), new LineResult(RemoveAnsiColors(result[2])));
                                        }

                                        var termMatches = GetTermMatches(result[2]);
                                        foreach (var termMatch in termMatches)
                                        {
                                            FileResults[(path, filename, lineNumber)].TermResults.Add(new TermResult(termIndex, termMatch));
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static readonly char[] PatternDelimiters = { ' ', ':', ';', ',' };

        private static readonly Dictionary<FileEncoding, string> EncodingTypes = new()
        {
            { FileEncoding.Auto, string.Empty },
            { FileEncoding.GBK, "GBK" },
        };

        private static IEnumerable<string> GetSearchPatterns(string patternString)
        {
            var searchPatterns = new List<string>();
            var splitPatternString = patternString.Split(PatternDelimiters, StringSplitOptions.RemoveEmptyEntries);

            var invalidChars = Path.GetInvalidFileNameChars().Where(x => x != Path.DirectorySeparatorChar && x != '*').ToList();
            invalidChars.Add('{');
            invalidChars.Add('}');

            foreach (var token in splitPatternString)
            {
                var pattern = token;

                // Remove any invalid characters from patterns.
                foreach (var c in invalidChars)
                {
                    pattern = pattern.Replace(c.ToString(), string.Empty);
                }

                // Remove any whitespace from patterns.
                pattern = Regex.Replace(pattern, @"\s+", "");

                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    searchPatterns.Add(pattern);
                }
            }

            return searchPatterns;
        }

        private static string RemoveAnsiColors(string source)
        {
            return Regex.Replace(source, @"\x1B\[[^@-~]*[@-~]", string.Empty);
        }

        private static IList<Range> GetTermMatches(string source)
        {
            var ripGrepMatches = Regex.Matches(source, @"\x1B\[0m\x1B\[1m\x1B\[31m(.+?)\x1B\[0m");

            var termMatches = new List<Range>();

            var processIndex = 0;
            var originalStringIndex = 0;
            for (var i = 0; i < ripGrepMatches.Count; i++)
            {
                if (processIndex != ripGrepMatches[i].Groups[0].Index)
                {
                    originalStringIndex += (ripGrepMatches[i].Groups[0].Index - processIndex);
                }

                var start = originalStringIndex;
                originalStringIndex += ripGrepMatches[i].Groups[1].Value.Length;
                termMatches.Add(new Range(start, originalStringIndex - 1));
                processIndex = ripGrepMatches[i].Index + ripGrepMatches[i].Length;
            }

            return termMatches;
        }
    }
}
