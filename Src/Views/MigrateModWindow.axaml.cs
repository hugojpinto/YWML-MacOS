using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Newtonsoft.Json;
using YWML.Src.FAMerger;
using YWML.Src.Loader.DataClasses;
using YWML.Src.Utils.Dialogs;

namespace YWML.Src.Views
{
    public partial class MigrateModWindow : Window
    {
        private string _modFolder = "";
        public string _romfsFolder = "";

        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly DispatcherTimer timer = new DispatcherTimer();

        public MigrateModWindow()
        {
            InitializeComponent();

            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            Closed += (_, _) => timer.Stop();
        }

        private async Task<string?> SelectFolder(string desc)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = desc,
                AllowMultiple = false,
            });
            if (folders.Count == 0) return null;
            return folders[0].TryGetLocalPath();
        }

        private async void ChooseModBtn_Click(object? sender, RoutedEventArgs e)
        {
            var f = await SelectFolder("Select your mod folder (should contain an FA file or two)");
            if (f != null) _modFolder = f;
            if (!string.IsNullOrEmpty(_modFolder))
                ModSelectedLbl.Text = new DirectoryInfo(_modFolder).Name + " selected.";
        }

        private async void ChooseRomfsBtn_Click(object? sender, RoutedEventArgs e)
        {
            var f = await SelectFolder("Select the game's original RomFS folder (should contain an FA file or two)");
            if (f != null) _romfsFolder = f;
            if (!string.IsNullOrEmpty(_romfsFolder))
                OgRomfsLabel.Text = new DirectoryInfo(_romfsFolder).Name + " selected.";
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            TimeElapsedLabel.Text = $"Time Elapsed: {stopwatch.Elapsed:mm\\:ss\\.ff}";
        }

        private async Task<int> ExportDiffsIntoYWMLMod(string modpath)
        {
            stopwatch.Start();
            timer.Start();
            //create YWML project json
            var proj = new CYwmlProject()
            {
                Name = ModNameTextBox.Text,
                Author = ModAuthorTextBox.Text,
                Version = ModVersionTextBox.Text,
            };
            //merge mods
            var merger = new CFAMerger(_modFolder, _romfsFolder);

            StatusLabel.Text = "Status: Scanning files...";

            int result = await Task.Run(() => merger.GetDiffs());

            StatusLabel.Text = "Status: Processing results...";
            switch (result)
            {
                case 0:
                    HandleYWMLProjFiles(modpath, merger, proj);
                    break;
                case 1:
                    stopwatch.Stop();
                    timer.Stop();
                    await CDialogs.ShowMessageAsync(CFAMerger.FA_MISMATCH_MESSAGE, this);
                    return 1;

            }

            return 0;
        }

        private void HandleYWMLProjFiles(string modpath, CFAMerger merger, CYwmlProject proj)
        {
            //write ywml proj
            var projJson = JsonConvert.SerializeObject(proj);
            File.WriteAllText(Path.Combine(modpath, "ywml.json"), projJson);
            //copy loose folders
            CopyMovSnd(modpath);
            //get all files from all mod FAs
            foreach (var f in merger.ChangedOrAddedFiles)
            {
                var modFilePath = modpath + "/include/" + f.FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(modFilePath)!);
                var modFile = merger.ModFAHandles[f.FAIdx].Directory.GetFileFromFullPath(f.FilePath);
                File.WriteAllBytes(modFilePath, modFile);
            }
            //clean up
            foreach (var handle in merger.ModFAHandles) handle.Close();
            stopwatch.Stop();
            timer.Stop();
        }

        void CopyDir(string src, string dst)
        {
            foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dir.Replace(src, dst));

            foreach (var file in Directory.GetFiles(src, "*.*", SearchOption.AllDirectories))
            {
                var fileDst = file.Replace(src, dst);
                Directory.CreateDirectory(Path.GetDirectoryName(fileDst)!);
                File.Copy(file, fileDst, true);
            }
        }

        private void CopyMovSnd(string dst)
        {
            foreach (var folder in Directory.GetDirectories(_modFolder))
            {
                string? dirName = new DirectoryInfo(folder).Name;
                if (dirName == "mov" || dirName == "snd")
                {
                    CopyDir(folder, dst + "/" + dirName);
                }
            }
        }

        //migrate
        private async void MigrateBtn_Click(object? sender, RoutedEventArgs e)
        {
            stopwatch.Reset();
            TimeElapsedLabel.Text = "Time elapsed: None";
            //check that fields were filled
            if (String.IsNullOrEmpty(ModNameTextBox.Text)
                || String.IsNullOrEmpty(ModAuthorTextBox.Text)
                || String.IsNullOrEmpty(ModVersionTextBox.Text)
                || String.IsNullOrEmpty(_modFolder)
                || String.IsNullOrEmpty(_romfsFolder))
            {
                await CDialogs.ShowMessageAsync("Please fill out all the fields.", this);
                return;
            }

            //select output mod folder
            await CDialogs.ShowMessageAsync("Please choose a folder where your mod will be saved.", this);
            var modpath = await SelectFolder("Choose your output mod folder");
            if (modpath == null)
            {
                await CDialogs.ShowMessageAsync("Operation was cancelled.", this);
                return;
            }

            SetControlsEnabled(false);
            int result = await ExportDiffsIntoYWMLMod(modpath);
            StatusLabel.Text = "Status: Waiting on user";
            SetControlsEnabled(true);
            if (result == 0)
            {
                await CDialogs.ShowMessageAsync("Done!", this);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            MigrateBtn.IsEnabled = enabled;
            ChooseRomfsBtn.IsEnabled = enabled;
            ChooseModBtn.IsEnabled = enabled;
            ModNameTextBox.IsEnabled = enabled;
            ModAuthorTextBox.IsEnabled = enabled;
            ModVersionTextBox.IsEnabled = enabled;
        }
    }
}
