using Avalonia.Controls;
using Avalonia.Interactivity;
using YWML.Src.ConfigManager;
using YWML.Src.Utils.Dialogs;
using YWML.Src.Utils.GeneralUtils;
using YWML.Src.Utils.Platform;

namespace YWML.Src.Views
{
    public partial class MainWindow : Window
    {
        private bool _startupDone;

        public MainWindow()
        {
            InitializeComponent();
            VerLabel.Text = CGeneralUtils.APP_VERSION;

            // Nothing may run before the config bootstrap has finished.
            SetMenuEnabled(false);
            Opened += MainWindow_Opened;
        }

        /// <summary>
        /// The bootstrap that used to live in Program.Main. It runs once the window exists so the
        /// "first boot after update" prompt has something to parent itself to.
        /// </summary>
        private async void MainWindow_Opened(object? sender, EventArgs e)
        {
            if (_startupDone) return;
            _startupDone = true;

            await CConfigManager.InitializeAsync();

            //New user
            if (!Directory.Exists(CGeneralUtils.YWMLDataDir))
            {
                CConfigManager.Cfg.IsUpdateFirstBoot = false;
                CConfigManager.UpdateConfig();
            }
            //Old user who updated
            if (CConfigManager.Cfg.IsUpdateFirstBoot)
            {
                await CDialogs.ShowMessageAsync(
                    "YWML 1.1 introduced a restructure to the extension library, which means it unfortunately has to erase all previously installed extensions from your computer.\nDon't worry, you can reinstall all the extensions from the new and improved extension library!",
                    this);
                Directory.Delete(CGeneralUtils.YWMLDataDir, true);
                CConfigManager.Cfg.IsUpdateFirstBoot = false;
                CConfigManager.UpdateConfig();
            }

            Directory.CreateDirectory(CGeneralUtils.YWMLDataDir);
            SetMenuEnabled(true);
        }

        private void SetMenuEnabled(bool enabled)
        {
            LoadButton.IsEnabled = enabled;
            ExtLibButton.IsEnabled = enabled;
            ConfigOpenButton.IsEnabled = enabled;
            MigrateButton.IsEnabled = enabled;
        }

        private async void ExtLibButton_Click(object? sender, RoutedEventArgs e)
        {
            await new ExtensionLibraryWindow().ShowDialog(this);
        }

        private async void LoadButton_Click(object? sender, RoutedEventArgs e)
        {
            await new LoadWindow().ShowDialog(this);
        }

        private async void ConfigOpenButton_Click(object? sender, RoutedEventArgs e)
        {
            var filePath = Path.GetFullPath(CGeneralUtils.WritableConfigPath);
            if (File.Exists(filePath))
            {
                try
                {
                    CPlatformUtils.RevealInFileManager(filePath);
                }
                catch (Exception ex)
                {
                    await CDialogs.ShowMessageAsync($"Could not open the file manager: {ex.Message}\n\nThe config file is at:\n{filePath}", this);
                }
            }
            else
            {
                await CDialogs.ShowMessageAsync("Config file is not found.", this);
            }
        }

        private async void MigrateButton_Click(object? sender, RoutedEventArgs e)
        {
            await new MigrateModWindow().ShowDialog(this);
        }
    }
}
