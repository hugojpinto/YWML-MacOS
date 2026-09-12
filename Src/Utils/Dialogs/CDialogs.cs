using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using YWML.Src.Views;

namespace YWML.Src.Utils.Dialogs
{
    public enum EDialogButtons
    {
        Ok,
        YesNo,
    }

    public enum EDialogResult
    {
        Ok,
        Yes,
        No,
    }

    /// <summary>
    /// Cross platform stand-in for the WinForms <c>MessageBox.Show</c> calls of the original app.
    /// Everything is async because Avalonia has no blocking modal dialog.
    /// </summary>
    public static class CDialogs
    {
        public const string DEFAULT_TITLE = "YWML";

        public static async Task<EDialogResult> ShowAsync(
            string message,
            string title = DEFAULT_TITLE,
            EDialogButtons buttons = EDialogButtons.Ok,
            Window? owner = null)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                var marshalled = new TaskCompletionSource<EDialogResult>();
                Dispatcher.UIThread.Post(async () =>
                {
                    try { marshalled.TrySetResult(await ShowAsync(message, title, buttons, owner)); }
                    catch (Exception ex) { marshalled.TrySetException(ex); }
                });
                return await marshalled.Task;
            }

            var dialog = new MessageBoxWindow(message, title, buttons);
            owner ??= FindOwner();

            if (owner != null && owner.IsVisible)
            {
                return await dialog.ShowDialog<EDialogResult>(owner);
            }

            // No parent window yet (startup path): show it standalone.
            var tcs = new TaskCompletionSource<EDialogResult>();
            dialog.Closed += (_, _) => tcs.TrySetResult(dialog.Result);
            dialog.Show();
            return await tcs.Task;
        }

        /// <summary>Convenience wrapper matching the original <c>MessageBox.Show(text)</c> usage.</summary>
        public static Task ShowMessageAsync(string message, Window? owner = null)
            => ShowAsync(message, DEFAULT_TITLE, EDialogButtons.Ok, owner);

        public static async Task<bool> ShowYesNoAsync(string message, string title = DEFAULT_TITLE, Window? owner = null)
            => await ShowAsync(message, title, EDialogButtons.YesNo, owner) == EDialogResult.Yes;

        private static Window? FindOwner()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return null;

            return desktop.Windows.FirstOrDefault(w => w.IsActive && w.IsVisible)
                   ?? desktop.Windows.LastOrDefault(w => w.IsVisible)
                   ?? desktop.MainWindow;
        }
    }
}
