using System.Text;
using Avalonia;

namespace YWML.Src
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            // Needed by the Level-5 archive code for the legacy Shift-JIS / CP1252 code pages.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // The first boot / config bootstrap that used to live here now runs from
            // MainWindow.OnOpened, because Avalonia dialogs need a running UI thread.
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by the visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
