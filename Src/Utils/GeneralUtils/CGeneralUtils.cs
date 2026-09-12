namespace YWML.Src.Utils.GeneralUtils
{
    public static class CGeneralUtils
    {
        public const string APP_VERSION = "1.1.0";
        private static string ExeDir = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// Per-user writable data directory.
        /// Windows: %APPDATA%\YWML
        /// macOS:   ~/Library/Application Support/YWML
        /// Linux:   ~/.config/YWML (or $XDG_CONFIG_HOME/YWML)
        /// </summary>
        public static string YWMLDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
                                      Environment.SpecialFolderOption.Create),
            "YWML");

        public static string ExtensionInstallDirectory = Path.Combine(YWMLDataDir, "extensions_install");
        public static string ExtensionInstalledList = Path.Combine(ExtensionInstallDirectory, "installed_ext.json");
        public static string ExtensionLibraryCachePath = Path.Combine(YWMLDataDir, "extension_lib_cache");
        public static string TmpDirectory = Path.Combine(YWMLDataDir, "tmp");
        public static string WritableConfigPath = Path.Combine(YWMLDataDir, "config.toml");
        public static string DefaultInstallationDirectoriesPath = Path.Combine(YWMLDataDir, "default_install_dirs.json");
    }
}
