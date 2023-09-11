using FramePFX.Themes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace rg_gui
{
    public static class TextBlockFormatter
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(TextBlockFormatter),
            new PropertyMetadata(null, OnTextChanged));

        public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);

        public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);

        private static void OnTextChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = (TextBlock)o;
            if (textBlock == null)
            {
                return;
            }

            textBlock.Inlines.Clear();

            var value = (string)e.NewValue;
            if (value == null)
            {
                return;
            }

            var matches = Regex.Matches(value, @"<c(\d)>(.+?)</c[\d]>");
            var startingIndex = 0;

            for (var i = 0; i < matches.Count; i++)
            {
                if (startingIndex != matches[i].Groups[0].Index)
                {
                    textBlock.Inlines.Add(new Run(UnescapeString(value.Substring(startingIndex, matches[i].Groups[0].Index - startingIndex))));
                }

                textBlock.Inlines.Add(new Run(UnescapeString(matches[i].Groups[2].Value)) { Background = new SolidColorBrush((Color)ThemesController.GetResource($"AColour.DataGrid.TextHighlightBackground{matches[i].Groups[1].Value}")) });
                startingIndex = matches[i].Index + matches[i].Length;
            }

            textBlock.Inlines.Add(new Run(UnescapeString(value.Substring(startingIndex))));
        }

        private static string UnescapeString(string source)
        {
            return source.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
        }
    }
}