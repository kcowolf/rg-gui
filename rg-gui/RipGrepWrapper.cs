// TODO -- Add file/line result counts, display in UI
// TODO -- Improve right-click support (only works if all files in the same folder.)
// TODO -- Save splitter position.

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
        public class SearchParameters
        {
            public string StartPath { get; set; } = string.Empty;

            public string SearchString { get; set; } = string.Empty;

            public string IncludePatterns { get; set; } = string.Empty;

            public string ExcludePatterns { get; set; } = string.Empty;

            public bool IncludeHiddenFiles { get; set; } = true;

            public bool IgnoreCase { get; set; } = true;

            public bool Recursive { get; set; } = true;
        }

        public class ResultLine
        {
            public ResultLine(int lineNumber, string lineContent)
            {
                LineNumber = lineNumber;
                LineContent = lineContent;
            }

            public int LineNumber { get; }
            public string LineContent { get; }
        }

        public readonly ConcurrentDictionary<(string path, string filename, int index), List<ResultLine>> Results = new();

        public event EventHandler<(string path, string filename, int index)>? FileFound;
        protected void RaiseFileFound(string path, string filename, int index)
        {
            FileFound?.Invoke(this, (path, filename, index));
        }

        private string m_ripGrepPath;

        public RipGrepWrapper(string ripGrepPath)
        {
            m_ripGrepPath = ripGrepPath;
        }

        public void Clear()
        {
            Results.Clear();
        }

        public async Task Search(SearchParameters searchParameters, CancellationToken cancellationToken, int index)
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

            if (!string.IsNullOrWhiteSpace(searchParameters.IncludePatterns))
            {
                argsBuilder.Append("-g={");
                argsBuilder.AppendJoin(",", GetSearchPatterns(searchParameters.IncludePatterns));
                argsBuilder.Append("} ");
            }

            if (searchParameters.ExcludePatterns.Any())
            {
                argsBuilder.Append("-g=!{");
                argsBuilder.AppendJoin(",", GetSearchPatterns(searchParameters.ExcludePatterns));
                argsBuilder.Append("} ");
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
                await foreach (var cmdEvent in cmd.ListenAsync(cancellationToken))
                {
                    switch (cmdEvent)
                    {
                        case StandardOutputCommandEvent stdOut:
                            {
                                var result = stdOut.Text.Split(fieldMatchSeparator, 3);

                                var path = Path.GetDirectoryName(result[0]) ?? string.Empty;
                                var filename = Path.GetFileName(result[0]) ?? string.Empty;

                                if (!Results.ContainsKey((path, filename, index)))
                                {
                                    Results.GetOrAdd((path, filename, index), new List<ResultLine>());
                                    RaiseFileFound(path, filename, index);
                                }

                                if (result.Length == 3)
                                {
                                    Results[(path, filename, index)].Add(new ResultLine(int.Parse(result[1]), result[2]));
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

        private readonly char[] PatternDelimiters = { ' ', ':', ';', ',' };

        private IEnumerable<string> GetSearchPatterns(string patternString)
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
    }
}
