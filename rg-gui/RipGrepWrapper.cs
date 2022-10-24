// TODO -- Need to add safety-checks for setting arguments
// TODO -- Add file/line result counts, display in UI
// TODO -- Support recursive flag -- set max-depth to 1 if false
// TODO -- Allow files to be right-clicked, display Windows context menu
// TODO -- Change grid display (remove inner borders)
// TODO -- Need to figure out exceptions -- no results throws an exception and terminates the app

using CliWrap;
using CliWrap.EventStream;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

            public IEnumerable<string> IncludePatterns { get; set; } = Enumerable.Empty<string>();

            public IEnumerable<string> ExcludePatterns { get; set; } = Enumerable.Empty<string>();

            public bool IgnoreCase { get; set; }

            public bool Recursive { get; set; }
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

        public readonly ConcurrentDictionary<(string path, string filename), List<ResultLine>> Results = new();

        public event EventHandler<(string path, string filename)>? FileFound;
        protected void RaiseFileFound(string path, string filename)
        {
            FileFound?.Invoke(this, (path, filename));
        }

        private string m_ripGrepPath;

        public RipGrepWrapper(string ripGrepPath)
        {
            m_ripGrepPath = ripGrepPath;
        }

        public async Task Search(SearchParameters searchParameters, CancellationToken cancellationToken)
        {
            const string fieldMatchSeparator = "\t";

            Results.Clear();

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

            if (searchParameters.IncludePatterns.Any())
            {
                argsBuilder.Append("-g={");
                argsBuilder.Append(string.Join(",", searchParameters.IncludePatterns));
                argsBuilder.Append("} ");
            }

            if (searchParameters.ExcludePatterns.Any())
            {
                argsBuilder.Append("-g=!{");
                argsBuilder.Append(string.Join(",", searchParameters.ExcludePatterns));
                argsBuilder.Append("} ");
            }

            if (!string.IsNullOrWhiteSpace(searchParameters.SearchString))
            {
                argsBuilder.Append(searchParameters.SearchString);
                argsBuilder.Append(' ');
            }

            argsBuilder.Append(searchParameters.StartPath);

            var cmd = Cli.Wrap(m_ripGrepPath)
                .WithArguments(argsBuilder.ToString());
            
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

                                if (!Results.ContainsKey((path, filename)))
                                {
                                    Results.GetOrAdd((path, filename), new List<ResultLine>());
                                    RaiseFileFound(path, filename);
                                }

                                if (result.Length == 3)
                                {
                                    Results[(path, filename)].Add(new ResultLine(int.Parse(result[1]), result[2]));
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
    }
}
