using Newtonsoft.Json;
using YWML.Src.ConfigManager;
using YWML.Src.ExtensionLibrary.DataClasses;
using YWML.Src.Utils.Dialogs;
using YWML.Src.Utils.GeneralUtils;

namespace YWML.Src.ExtensionLibrary
{
    public class CExtensionLibrary
    {
        public CExtensionLibInfo ExtensionInfo = new();
        //string is id
        public Dictionary<string, CExtensionLibraryItem> InstalledList = new();

        public async Task LoadInstalledListAsync()
        {
            string installedListJson = string.Empty;
            if (File.Exists(CGeneralUtils.ExtensionInstalledList))
            {
                installedListJson = File.ReadAllText(CGeneralUtils.ExtensionInstalledList);
            }

            if (installedListJson != string.Empty)
            {
                try
                {
                    InstalledList = JsonConvert.DeserializeObject<Dictionary<string, CExtensionLibraryItem>>(installedListJson)!;
                }
                catch
                {
                    await CDialogs.ShowMessageAsync("Invalid installed list. Resetting");
                    InstalledList = new();
                }
                if (InstalledList == null)
                {
                    await CDialogs.ShowMessageAsync("Invalid installed list. Resetting");
                    InstalledList = new();
                }
            }
        }

        public async Task FetchDataAsync()
        {
            bool useCache = false;
            string extensionLibraryJson = string.Empty;
            var cachePath = CGeneralUtils.ExtensionLibraryCachePath;
            //Get the extension library
            HttpClient client = new()
            {
                BaseAddress = new Uri(CConfigManager.Cfg.ExtensionLibraryURL),
            };
            try
            {
                extensionLibraryJson = await client.GetStringAsync(string.Empty);
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                File.WriteAllText(cachePath, extensionLibraryJson);
            }
            catch
            {
                var result = await CDialogs.ShowYesNoAsync("An error occurred while fetching the latest extension library. Would you like to try using a cached extension library (It may be missing new extension or even straight up including broken links)?", "Error");
                if (result)
                {
                    if (File.Exists(cachePath))
                    {
                        await CDialogs.ShowMessageAsync("Cache available. Loading now");
                        useCache = true;
                    }
                    else
                    {
                        await CDialogs.ShowMessageAsync("Sorry, there isn't a previously cached version of the extension library on this device.");
                        throw new HttpRequestException();
                    }
                }
                else
                {
                    throw new HttpRequestException();
                }
            }

            if (useCache)
            {
                extensionLibraryJson = File.ReadAllText(cachePath);
            }
            ExtensionInfo = JsonConvert.DeserializeObject<CExtensionLibInfo>(extensionLibraryJson)!;
        }
    }
}
