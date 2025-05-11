using System.Reflection;
using System.Windows.Controls;
using System;
using System.Windows;

namespace rg_gui
{
    // https://stackoverflow.com/a/45627524

    public class SelectableTextBlock : TextBlock
    {
        // Reflection types, properties, and methods
        private static readonly Type TextEditorType = Type.GetType("System.Windows.Documents.TextEditor, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        private static readonly PropertyInfo TextEditorIsReadOnly = TextEditorType.GetProperty("IsReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo TextEditorTextView = TextEditorType.GetProperty("TextView", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo TextEditorRegisterCommandHandlersMethod = TextEditorType.GetMethod("RegisterCommandHandlers", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(Type), typeof(bool), typeof(bool), typeof(bool) }, null);

        private static readonly Type TextContainerType = Type.GetType("System.Windows.Documents.ITextContainer, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        private static readonly PropertyInfo TextContainerTextView = TextContainerType.GetProperty("TextView");

        private static readonly PropertyInfo TextBlockTextContainer = typeof(TextBlock).GetProperty("TextContainer", BindingFlags.Instance | BindingFlags.NonPublic);

        static SelectableTextBlock()
        {
            // Make this control focusable
            FocusableProperty.OverrideMetadata(typeof(SelectableTextBlock), new FrameworkPropertyMetadata(true));

            // Register class event handlers
            // (Type controlType, bool acceptsRichContent, bool readOnly, bool registerEventListeners)
            TextEditorRegisterCommandHandlersMethod.Invoke(null, new object[] { typeof(SelectableTextBlock), false, true, true });
        }

        private readonly object _textEditor;

        public SelectableTextBlock()
        {
            var textContainer = TextBlockTextContainer.GetValue(this);

            // Create TextEditor instance, assign the TextContainer to it.
            // (ITextContainer textContainer, FrameworkElement uiScope, bool isUndoEnabled)
            _textEditor = Activator.CreateInstance(TextEditorType, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, new[] { textContainer, this, false }, null);

            // Set IsReadOnly and TextView properties.
            var textView = TextContainerTextView.GetValue(textContainer);
            TextEditorIsReadOnly.SetValue(_textEditor, true);
            TextEditorTextView.SetValue(_textEditor, textView);
        }
    }
}
