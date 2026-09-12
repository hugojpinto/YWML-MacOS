using YWML.Src.Utils.GeneralUtils;

namespace YWML.Src.ExtensionLibrary.DataClasses
{
    public class CExtension
    {
        public string Name { get; set; }
        //in mb
        public int FileSize { get; set; }

        //Download link for the LZMA extension 
        public string Link { get; set; }
        public string Id { get; set; }

        //Title ID used on the console for the game to generate the mod folder
        public string TitleId { get; set; }

        //Original FA name
        public string OgFAName { get; set; }

        /// <param name="setBusy">Called with true while work is in progress, false when done (used to lock buttons).</param>
        /// <param name="status">Receives human readable status updates.</param>
        public Task UninstallAsync(Action<bool> setBusy, IProgress<string> status, Dictionary<string, CExtensionLibraryItem> installedList)
        {
            setBusy(true);
            installedList.Remove(Id);
            var installDir = Path.Combine(CGeneralUtils.ExtensionInstallDirectory, Id);
            if (Directory.Exists(installDir)) Directory.Delete(installDir, true);
            status.Report("Finished uninstalling extension");
            setBusy(false);
            return Task.CompletedTask;
        }

        /// <param name="setBusy">Called with true while work is in progress, false when done (used to lock buttons).</param>
        /// <param name="status">Receives human readable status updates.</param>
        /// <param name="percentage">Receives download progress as a percentage string ("" when idle).</param>
        public async Task InstallAsync(Action<bool> setBusy, IProgress<string> status, IProgress<string> percentage, Dictionary<string, CExtensionLibraryItem> installedList)
        {
            setBusy(true);
            status.Report("Downloading LZMA extension");
            var compressedPath = Path.Combine(CGeneralUtils.TmpDirectory, "compressed.7z");
            Directory.CreateDirectory(CGeneralUtils.TmpDirectory);
            //Download compressed FA
            await DownloadCompressedFAAsync(compressedPath, percentage);
            //Unpack it
            percentage.Report("");
            status.Report("Unpacking LZMA extension");
            var decompressedBytes = await DecompressAsync(await File.ReadAllBytesAsync(compressedPath));
            //Write decompressed FA to disk
            status.Report("Installing unpacked extension");
            var unpackedFaPath = Path.Combine(Path.Combine(CGeneralUtils.ExtensionInstallDirectory, Id), "patchable.fa");
            Directory.CreateDirectory(Path.GetDirectoryName(unpackedFaPath)!);
            await File.WriteAllBytesAsync(unpackedFaPath, decompressedBytes);
            Directory.Delete(CGeneralUtils.TmpDirectory, true);


            if (!installedList.Keys.Contains(this.Id))
            {
                installedList[this.Id] = new()
                {
                    FAName = this.OgFAName,
                    Name = this.Name,
                    TitleId = this.TitleId
                };
            }

            //Add to installed list
            status.Report("Finished installing extension!");
            setBusy(false);
        }

        private async Task DownloadCompressedFAAsync(string compressedPath, IProgress<string> percentage)
        {
            var client = new HttpClient();
            var response = await client.GetAsync(Link, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;

            using (var fs = new FileStream(compressedPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                {
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;

                        if (totalBytes > 0)
                        {
                            var percent = (int)((downloadedBytes * 100) / totalBytes);
                            percentage.Report($"{percent}%");
                        }
                    }
                }
            }
        }

        public async Task<byte[]> DecompressAsync(byte[] toDecompress)
        {
            var decompressed = await Task.Run(() => LZMA.Engine.Decompress(toDecompress));
            return decompressed;
        }

    }
}
