using ConcurrentCollections;
using FramePFX.Themes;
using Ookii.Dialogs.Wpf;
using Peter;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static rg_gui.RipGrepWrapper;

namespace rg_gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double DEFAULT_MAINWINDOW_LEFT = 0;
        private const double DEFAULT_MAINWINDOW_TOP = 0;
        private const double DEFAULT_MAINWINDOW_WIDTH = 800;
        private const double DEFAULT_MAINWINDOW_HEIGHT = 450;
        private const int DEFAULT_MAINWINDOW_STATE = 0;

        private const string DEFAULT_BASEPATH = "";
        private const string DEFAULT_INCLUDEFILES = "";
        private const string DEFAULT_EXCLUDEFILES = "";
        private const string DEFAULT_CONTAININGTEXT = "";

        private const bool DEFAULT_CASESENSITIVE = false;
        private const bool DEFAULT_RECURSIVE = true;
        private const bool DEFAULT_REGULAREXPRESSION = false;

        private const string DEFAULT_FILEENCODING = "Auto";

        private const int DEFAULT_MAXFILESIZE = 0;
        private const string DEFAULT_MAXFILESIZEUNIT = "None";

        private const int DEFAULT_MAXSEARCHTERMS = 10;

        private string m_currentInput = string.Empty;
        private string? m_currentSuggestion = string.Empty;
        private string m_currentText = string.Empty;
        private int m_selectionStart;
        private int m_selectionLength;
        private IEnumerable<string> m_folderSuggestionValues = Enumerable.Empty<string>();

        private readonly int m_maxSearchTerms;

        private const ThemeType DEFAULT_THEME = ThemeType.Light;
        private ThemeType m_currentTheme;

        public class FileSearchResult
        {
            public string Path { get; }

            public string Filename { get; }

            public FileSearchResult(string path, string filename)
            {
                Path = path;
                Filename = filename;
            }
        }

        public class ResultLine
        {
            public int Line { get; }

            public string Content { get; }

            public ResultLine(int line, string content)
            {
                Line = line;
                Content = content;
            }
        }

        private CancellationTokenSource? m_cancellationTokenSource;

        private readonly RipGrepWrapper m_ripGrepWrapper;

        public RangeObservableCollection<FileSearchResult> FileResultItems = new();
        public RangeObservableCollection<ResultLine> ResultLineItems = new();

        private readonly ConcurrentHashSet<(string, string, int)> m_fileResults = new();
        private int m_searchInstanceCount;

        public MainWindow()
        {
            InitializeComponent();

            var config = GetConfiguration();
            Left = double.TryParse(config.AppSettings.Settings["MainWindowLeft"]?.Value, out var left) ? left : DEFAULT_MAINWINDOW_LEFT;
            Top = double.TryParse(config.AppSettings.Settings["MainWindowTop"]?.Value, out var top) ? top : DEFAULT_MAINWINDOW_TOP;
            Width = double.TryParse(config.AppSettings.Settings["MainWindowWidth"]?.Value, out var width) ? width : DEFAULT_MAINWINDOW_WIDTH;
            Height = double.TryParse(config.AppSettings.Settings["MainWindowHeight"]?.Value, out var height) ? height : DEFAULT_MAINWINDOW_HEIGHT;
            WindowState = int.TryParse(config.AppSettings.Settings["MainWindowState"]?.Value, out var windowState) ? WindowState : DEFAULT_MAINWINDOW_STATE;

            txtBasePath.Text = config.AppSettings.Settings["BasePath"]?.Value ?? DEFAULT_BASEPATH;
            txtIncludeFiles.Text = config.AppSettings.Settings["IncludeFiles"]?.Value ?? DEFAULT_INCLUDEFILES;
            txtExcludeFiles.Text = config.AppSettings.Settings["ExcludeFiles"]?.Value ?? DEFAULT_EXCLUDEFILES;
            txtContainingText.Text = config.AppSettings.Settings["ContainingText"]?.Value ?? DEFAULT_CONTAININGTEXT;
            chkCaseSensitive.IsChecked = bool.TryParse(config.AppSettings.Settings["CaseSensitive"]?.Value, out var caseSensitive) ? caseSensitive : DEFAULT_CASESENSITIVE;
            chkRecursive.IsChecked = bool.TryParse(config.AppSettings.Settings["Recursive"]?.Value, out var recursive) ? recursive : DEFAULT_RECURSIVE;

            var fileEncoding = cmbEncoding.FindName(config.AppSettings.Settings["FileEncoding"]?.Value ?? DEFAULT_FILEENCODING);
            if (fileEncoding != null)
            {
                cmbEncoding.SelectedItem = fileEncoding;
            }
            else
            {
                cmbEncoding.SelectedIndex = 0;
            }

            txtMaxFileSize.Text = (int.TryParse(config.AppSettings.Settings["MaxFileSize"]?.Value, out var maxFileSize) ? maxFileSize : DEFAULT_MAXFILESIZE).ToString();
            var maxFileSizeUnit = cmbFileSizeUnit.FindName(config.AppSettings.Settings["MaxFileSizeUnit"]?.Value ?? DEFAULT_MAXFILESIZEUNIT);
            if (maxFileSizeUnit != null)
            {
                cmbFileSizeUnit.SelectedItem = maxFileSizeUnit;
            }
            else
            {
                cmbFileSizeUnit.SelectedIndex = 0;
            }

            m_currentTheme = Enum.TryParse<ThemeType>(config.AppSettings.Settings["Theme"]?.Value, out var themeName) ? themeName : DEFAULT_THEME;
            ThemesController.SetTheme(m_currentTheme);

            var ripgrepPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty, "rg.exe");
            if (!File.Exists(ripgrepPath))
            {
                MessageBox.Show("rg.exe not found in installation path.", "Error");
                throw new Exception("rg.exe not found in installation path.");
            }

            m_ripGrepWrapper = new RipGrepWrapper(ripgrepPath);

            m_maxSearchTerms = int.TryParse(config.AppSettings.Settings["MaxSearchTerms"]?.Value, out var maxSearchTerms) ? maxSearchTerms : DEFAULT_MAXSEARCHTERMS;

            DataContext = m_ripGrepWrapper;
            gridFileResults.DataContext = FileResultItems;
            gridResultLines.DataContext = ResultLineItems;

            m_ripGrepWrapper.FileFound += OnFileAdded;
        }

        private static Configuration GetConfiguration()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configPath = $"{appData}\\kcowolf\\rg-gui\\rg-gui.exe.config";

            var fileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = configPath
            };
            return ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
        }

        private static void SetConfigValue(Configuration config, string key, string value)
        {
            if (config.AppSettings.Settings[key] != null)
            {
                config.AppSettings.Settings[key].Value = value;
            }
            else
            {
                config.AppSettings.Settings.Add(key, value);
            }
        }

        private void OnClosing(object? sender, EventArgs e)
        {
            var config = GetConfiguration();
            SetConfigValue(config, "MainWindowLeft", Left.ToString());
            SetConfigValue(config, "MainWindowTop", Top.ToString());
            SetConfigValue(config, "MainWindowWidth", Width.ToString());
            SetConfigValue(config, "MainWindowHeight", Height.ToString());
            SetConfigValue(config, "MainWindowState", ((int)WindowState).ToString());

            SetConfigValue(config, "BasePath", txtBasePath.Text);
            SetConfigValue(config, "IncludeFiles", txtIncludeFiles.Text);
            SetConfigValue(config, "ExcludeFiles", txtExcludeFiles.Text);
            SetConfigValue(config, "ContainingText", txtContainingText.Text);
            SetConfigValue(config, "CaseSensitive", (chkCaseSensitive.IsChecked ?? DEFAULT_CASESENSITIVE).ToString());
            SetConfigValue(config, "Recursive", (chkRecursive.IsChecked ?? DEFAULT_RECURSIVE).ToString());
            SetConfigValue(config, "RegularExpression", (chkRegularExpression.IsChecked ?? DEFAULT_REGULAREXPRESSION).ToString());

            SetConfigValue(config, "FileEncoding", ((ComboBoxItem)cmbEncoding.SelectedItem).Name);
            SetConfigValue(config, "MaxFileSize", txtMaxFileSize.Text);
            SetConfigValue(config, "MaxFileSizeUnit", ((ComboBoxItem)cmbFileSizeUnit.SelectedItem).Name);
            SetConfigValue(config, "Theme", m_currentTheme.ToString());
            config.Save();

            ConfigurationManager.RefreshSection("appSettings");
        }

        private void OnFileAdded(object? sender, (string path, string filename, int index) result)
        {
            m_fileResults.Add((result.path, result.filename, result.index));

            // If result not present in all lists, return.
            for (int i = 0; i < m_searchInstanceCount; i++)
            {
                if (!m_fileResults.Contains((result.path, result.filename, i)))
                {
                    return;
                }
            }

            // If we reach this point, result was found by all search instances.
            Application.Current.Dispatcher.Invoke(delegate
            {
                // Ensure the same result won't be added multiple times.
                if (!FileResultItems.Any(x => x.Path == result.path && x.Filename == result.filename))
                {
                    FileResultItems.Add(new FileSearchResult(result.path, result.filename));
                    txtFileListStatus.Text = $"Found {FileResultItems.Count} files.";
                }
            });
        }

        private void gridFileResults_MouseDown(object? sender, MouseEventArgs e)
        {
            if ((e.RightButton == MouseButtonState.Pressed && !SystemParameters.SwapButtons) || (e.LeftButton == MouseButtonState.Pressed && SystemParameters.SwapButtons))
            {
                var selectedFiles = new List<FileInfo>();

                foreach (var selectedItem in gridFileResults.SelectedItems)
                {
                    if (selectedItem is FileSearchResult fileSearchResult)
                    {
                        selectedFiles.Add(new FileInfo(Path.Combine(fileSearchResult.Path, fileSearchResult.Filename)));
                    }
                }

                if (selectedFiles.Any())
                {
                    var point = PointToScreen(e.MouseDevice.GetPosition(this));

                    var shellContextMenu = new ShellContextMenu();
                    shellContextMenu.ShowContextMenu(selectedFiles, new System.Drawing.Point((int)point.X, (int)point.Y));
                }
            }
        }

        private void gridFileResults_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is FileSearchResult addedItem)
                {
                    ResultLineItems.Reset(Enumerable.Empty<ResultLine>());

                    for (int i = 0; i < m_searchInstanceCount; i++)
                    {
                        var resultLines = m_ripGrepWrapper.Results[(addedItem.Path, addedItem.Filename, i)];
                        foreach (var resultLine in resultLines)
                        {
                            if (!ResultLineItems.Any(x => x.Line == resultLine.LineNumber))
                            {
                                ResultLineItems.Add(new ResultLine(resultLine.LineNumber, resultLine.LineContent.Trim()));
                            }
                        }
                    }

                    txtResultLineStatus.Text = $"{ResultLineItems.Count} lines matched.";
                }
            }
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if ((txtBasePath.Text.IndexOfAny(Path.GetInvalidPathChars()) != -1) || !Directory.Exists(txtBasePath.Text))
            {
                MessageBox.Show("Invalid \"In Folder\" path.", "Error");
                return;
            }

            // based on https://stackoverflow.com/questions/66598956/how-to-stop-a-method-triggered-by-button-click-in-wpf

            if (m_cancellationTokenSource != null)
            {
                return;
            }

            // Based on https://stackoverflow.com/questions/52194058/regex-with-escaped-double-quotes
            var searchTerms = Regex.Matches(txtContainingText.Text, @"""[^""\\]*(?:\\.[^""\\]*)*""|([^\s])+|[^\s""]+");
            if (searchTerms.Count < 1)
            {
                return;
            }

            if (searchTerms.Count > m_maxSearchTerms)
            {
                MessageBox.Show($"Search text contains more than {m_maxSearchTerms} terms.");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            btnStart.IsEnabled = false;
            btnCancel.IsEnabled = true;
            var cancellationTokenSource = new CancellationTokenSource();
            m_cancellationTokenSource = cancellationTokenSource;

            m_searchInstanceCount = searchTerms.Count;
            m_fileResults.Clear();

            ResultLineItems.Reset(Enumerable.Empty<ResultLine>());
            txtFileListStatus.Text = string.Empty;
            txtResultLineStatus.Text = string.Empty;

            m_ripGrepWrapper.Clear();

            var startPath = txtBasePath.Text;
            if (startPath.EndsWith(Path.DirectorySeparatorChar))
            {
                startPath = startPath.TrimEnd(Path.DirectorySeparatorChar);
            }

            try
            {
                int index = 0;
                var ripGrepTasks = new List<Task>();
                foreach (var searchTerm in searchTerms.Cast<Match>())
                {
                    var searchParameters = new SearchParameters
                    {
                        StartPath = startPath,
                        SearchString = searchTerm.Value,
                        IgnoreCase = !(chkCaseSensitive.IsChecked ?? false),
                        Recursive = chkRecursive.IsChecked ?? true,
                        IncludePatterns = txtIncludeFiles.Text,
                        ExcludePatterns = txtExcludeFiles.Text,
                        RegularExpression = chkRegularExpression.IsChecked ?? false,
                        Encoding = (FileEncoding)cmbEncoding.SelectedIndex,
                        MaxFileSize = int.Parse(txtMaxFileSize.Text),
                        MaxFileSizeUnit = (MaxFileSizeUnit)cmbFileSizeUnit.SelectedIndex,
                    };

                    FileResultItems.Reset(Enumerable.Empty<FileSearchResult>());

                    ripGrepTasks.Add(m_ripGrepWrapper.Search(searchParameters, cancellationTokenSource.Token, index));

                    index++;
                }

                await Task.WhenAll(ripGrepTasks);
            }
            finally
            {
                btnCancel.IsEnabled = false;
                btnStart.IsEnabled = true;

                m_cancellationTokenSource = null;
                cancellationTokenSource.Cancel();
            }

            stopwatch.Stop();
            txtFileListStatus.Text = $"Found {FileResultItems.Count} files.  Took {stopwatch.Elapsed.TotalSeconds:0.00} seconds.";
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog()
            {
                Description = "Select folder",
                UseDescriptionForTitle = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                txtBasePath.Text = dialog.SelectedPath;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            m_cancellationTokenSource?.Cancel();
        }

        private void txtContainingText_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;

                if (btnStart.IsEnabled)
                {
                    btnStart.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            }
        }

        private void txtBasePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFolderSuggestionValues();

            // Based on https://learn.microsoft.com/en-us/answers/questions/840981/auto-complete-for-textbox-in-wpf-(mvvm)
            var input = txtBasePath.Text;
            if (input.Length > m_currentInput.Length && input != m_currentSuggestion)
            {
                m_currentSuggestion = m_folderSuggestionValues.FirstOrDefault(x => x.StartsWith(input, StringComparison.CurrentCultureIgnoreCase));
                if (m_currentSuggestion != null)
                {
                    m_currentText = m_currentSuggestion;
                    m_selectionStart = input.Length;
                    m_selectionLength = m_currentSuggestion.Length - input.Length;

                    txtBasePath.Text = m_currentText;
                    txtBasePath.Select(m_selectionStart, m_selectionLength);
                }
            }
            m_currentInput = input;
        }

        private void UpdateFolderSuggestionValues()
        {
            var input = txtBasePath.Text;

            if (input.EndsWith(Path.DirectorySeparatorChar) && Directory.Exists(input))
            {
                m_folderSuggestionValues = Directory.GetDirectories(input);
            }
        }

        private void txtMaxFileSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            var input = txtMaxFileSize.Text;
            txtMaxFileSize.Text = new string(input.Where(c => char.IsDigit(c)).ToArray());
        }

        private void cmbFileSizeUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtMaxFileSize.IsEnabled = (cmbFileSizeUnit.SelectedIndex != 0);
        }
    }
}
