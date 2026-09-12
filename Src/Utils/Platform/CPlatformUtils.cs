using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YWML.Src.Utils.Platform
{
    /// <summary>
    /// Small helpers for the handful of places where the original app assumed Windows.
    /// </summary>
    public static class CPlatformUtils
    {
        /// <summary>
        /// Root of the folder an emulator keeps its user data in.
        /// Windows: %APPDATA% (C:/Users/&lt;user&gt;/AppData/Roaming)
        /// macOS:   ~/Library/Application Support
        /// Linux:   $XDG_DATA_HOME or ~/.local/share
        /// </summary>
        public static string EmulatorDataRoot
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return $"C:/Users/{Environment.UserName}/AppData/Roaming";

                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return Path.Combine(home, "Library", "Application Support");

                var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                return !string.IsNullOrEmpty(xdg) ? xdg : Path.Combine(home, ".local", "share");
            }
        }

        /// <summary>
        /// The per-emulator folder name. Citra/Azahar &amp; friends use the product name verbatim on
        /// Windows and macOS, and a lowercase name on Linux.
        /// </summary>
        public static string EmulatorFolderName(string platformName)
            => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? platformName.ToLowerInvariant() : platformName;

        /// <summary>
        /// Builds the emulator's "load/mods/&lt;titleId&gt;" directory for the current OS.
        /// </summary>
        public static string EmulatorModDirectory(string platformName, string titleId)
        {
            var root = EmulatorDataRoot.Replace('\\', '/').TrimEnd('/');
            return $"{root}/{EmulatorFolderName(platformName)}/load/mods/{titleId}";
        }

        /// <summary>
        /// Message shown before asking the user to pick the microSD card root, tailored per OS.
        /// </summary>
        public static string MicroSdPrompt
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "Your 3DS microSD card must be inserted into your computer. Please insert it if you haven't already before proceeding.\n\n" +
                           "Click 'OK' to select your 3DS microSD card's volume when prompted. On macOS removable volumes are mounted under /Volumes " +
                           "(for example /Volumes/3DS), so pick the card's volume there and press \"Open\".";
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "Your 3DS microSD drive must be inserted into your computer. Please insert it if you haven't already before proceeding.\n\n" +
                           "Click 'OK' to select your 3DS microSD card's drive when prompted to in the file explorer. (For example: When inserting my microSD card, " +
                           "it showed up as the D:/ drive. So I went to \"This PC\", selected my D:/ drive and pressed \"open\". ";
                }
                return "Your 3DS microSD card must be inserted into your computer. Please insert it if you haven't already before proceeding.\n\n" +
                       "Click 'OK' to select your 3DS microSD card's mount point when prompted (usually somewhere under /media or /run/media).";
            }
        }

        /// <summary>
        /// Reveals a file in the OS file manager (the cross platform replacement for explorer.exe /select).
        /// </summary>
        public static void RevealInFileManager(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start(new ProcessStartInfo("open", new[] { "-R", filePath }) { UseShellExecute = false });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath.Replace('/', '\\')}\"",
                    UseShellExecute = true,
                });
            }
            else
            {
                var dir = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? filePath;
                Process.Start(new ProcessStartInfo("xdg-open", new[] { dir }) { UseShellExecute = false });
            }
        }
    }
}
