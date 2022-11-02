using Ookii.Dialogs.Wpf;
using Peter;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
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

        public MainWindow()
        {
            InitializeComponent();

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            txtBasePath.Text = config.AppSettings.Settings["BasePath"]?.Value ?? string.Empty;
            txtIncludeFiles.Text = config.AppSettings.Settings["IncludeFiles"]?.Value ?? string.Empty;
            txtExcludeFiles.Text = config.AppSettings.Settings["ExcludeFiles"]?.Value ?? string.Empty;
            txtContainingText.Text = config.AppSettings.Settings["ContainingText"]?.Value ?? string.Empty;
            chkCaseSensitive.IsChecked = bool.Parse(config.AppSettings.Settings["CaseSensitive"]?.Value ?? "false");
            chkRecursive.IsChecked = bool.Parse(config.AppSettings.Settings["Recursive"]?.Value ?? "true");

            m_ripGrepWrapper = new RipGrepWrapper(config.AppSettings.Settings["RipGrepPath"]?.Value ?? throw new Exception("RipGrepPath not set."));

            DataContext = m_ripGrepWrapper;
            gridFileResults.DataContext = FileResultItems;
            gridResultLines.DataContext = ResultLineItems;

            m_ripGrepWrapper.FileFound += OnFileAdded;
        }

        private void OnFileAdded(object? sender, (string path, string filename) result)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                FileResultItems.Add(new FileSearchResult(result.path, result.filename));
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

                    var resultLines = m_ripGrepWrapper.Results[(addedItem.Path, addedItem.Filename)];

                    foreach (var resultLine in resultLines)
                    {
                        ResultLineItems.Add(new ResultLine(resultLine.LineNumber, resultLine.LineContent.Trim()));
                    }
                }
            }
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            // based on https://stackoverflow.com/questions/66598956/how-to-stop-a-method-triggered-by-button-click-in-wpf

            if (m_cancellationTokenSource != null)
            {
                return;
            }

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["BasePath"].Value = txtBasePath.Text;
            config.AppSettings.Settings["IncludeFiles"].Value = txtIncludeFiles.Text;
            config.AppSettings.Settings["ExcludeFiles"].Value = txtExcludeFiles.Text;
            config.AppSettings.Settings["ContainingText"].Value = txtContainingText.Text;
            config.AppSettings.Settings["CaseSensitive"].Value = (chkCaseSensitive.IsChecked ?? false) ? "true" : "false";
            config.AppSettings.Settings["Recursive"].Value = (chkRecursive.IsChecked ?? true) ? "true" : "false";
            config.Save();
            ConfigurationManager.RefreshSection("appSettings");

            btnStart.IsEnabled = false;
            btnCancel.IsEnabled = true;
            var cancellationTokenSource = new CancellationTokenSource();
            m_cancellationTokenSource = cancellationTokenSource;
            
            try
            {
                var searchParameters = new SearchParameters
                {
                    StartPath = txtBasePath.Text,
                    SearchString = txtContainingText.Text,
                    IgnoreCase = !(chkCaseSensitive.IsChecked ?? false),
                    Recursive = chkRecursive.IsChecked ?? true,
                    IncludePatterns = txtIncludeFiles.Text,
                    ExcludePatterns = txtExcludeFiles.Text
                };

                FileResultItems.Reset(Enumerable.Empty<FileSearchResult>());

                await Task.Run(() =>
                {
                    return m_ripGrepWrapper.Search(searchParameters, cancellationTokenSource.Token);
                });
            }
            finally
            {
                btnCancel.IsEnabled = false;
                btnStart.IsEnabled = true;

                m_cancellationTokenSource = null;
                cancellationTokenSource.Cancel();
            }
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
    }
}
