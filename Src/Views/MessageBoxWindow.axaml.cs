using Avalonia.Controls;
using Avalonia.Interactivity;
using YWML.Src.Utils.Dialogs;

namespace YWML.Src.Views
{
    /// <summary>
    /// Minimal replacement for the WinForms MessageBox used ~27 times by the original app.
    /// </summary>
    public partial class MessageBoxWindow : Window
    {
        public EDialogResult Result { get; private set; } = EDialogResult.No;

        // Parameterless ctor for the XAML designer / previewer.
        public MessageBoxWindow() : this(string.Empty, CDialogs.DEFAULT_TITLE, EDialogButtons.Ok)
        {
        }

        public MessageBoxWindow(string message, string title, EDialogButtons buttons)
        {
            InitializeComponent();

            Title = title;
            MessageText.Text = message;

            if (buttons == EDialogButtons.YesNo)
            {
                YesButton.IsVisible = true;
                NoButton.IsVisible = true;
                OkButton.IsVisible = false;
                // Match the WinForms MessageBoxDefaultButton.Button2 behaviour of the warning prompt.
                Result = EDialogResult.No;
            }
            else
            {
                Result = EDialogResult.Ok;
            }
        }

        private void OkButton_Click(object? sender, RoutedEventArgs e) => CloseWith(EDialogResult.Ok);
        private void YesButton_Click(object? sender, RoutedEventArgs e) => CloseWith(EDialogResult.Yes);
        private void NoButton_Click(object? sender, RoutedEventArgs e) => CloseWith(EDialogResult.No);

        private void CloseWith(EDialogResult result)
        {
            Result = result;
            Close(result);
        }
    }
}
