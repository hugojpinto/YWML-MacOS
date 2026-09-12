using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Newtonsoft.Json;
using YWML.Src.ExtensionLibrary;
using YWML.Src.ExtensionLibrary.DataClasses;
using YWML.Src.Loader;
using YWML.Src.Loader.DataClasses;
using YWML.Src.Utils.Dialogs;
using YWML.Src.Utils.GeneralUtils;
using YWML.Src.Utils.Platform;

namespace YWML.Src.Views
{
    public partial class LoadWindow : Window
    {
        private readonly CExtensionLibrary _lib = new();
        private Dictionary<string, string> _defaultInstallDirs = new();
        private readonly Dictionary<string, string> _nameToId = new();
        private readonly Dictionary<string, string> _modNameToPath = new();
        private bool _initialized;

        public ObservableCollection<CModListEntry> Mods { get; } = new();

        public LoadWindow()
        {
            InitializeComponent();
            DataContext = this;

            PlatComboBox.ItemsSource = Enum.GetValues<SPlatform>();
            PlatComboBox.SelectedIndex = 0;

            ModInstallPathTextBox.TextChanged += ModInstallPathTextBox_TextChanged;
            Opened += LoadWindow_Opened;
            Closing += LoadWindow_Closing;
        }

        private async void LoadWindow_Opened(object? sender, EventArgs e)
        {
            if (_initialized) return;
            _initialized = true;

            await _lib.LoadInstalledListAsync();

            //Load the installed extensions into the comboBox
            var names = new List<string>();
            foreach (var key in _lib.InstalledList.Keys)
            {
                _nameToId[_lib.InstalledList[key].Name] = key;
                names.Add(_lib.InstalledList[key].Name);
            }
            ExtensionComboBox.ItemsSource = names;

            if (!File.Exists(CGeneralUtils.DefaultInstallationDirectoriesPath))
            {
                _defaultInstallDirs = new();
            }
            else
            {
                _defaultInstallDirs =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        File.ReadAllText(CGeneralUtils.DefaultInstallationDirectoriesPath)) ?? new();
            }
        }

        private void LoadWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            var defaultInstallDirsJson = JsonConvert.SerializeObject(_defaultInstallDirs);
            Directory.CreateDirectory(Path.GetDirectoryName(CGeneralUtils.DefaultInstallationDirectoriesPath)!);
            File.WriteAllText(CGeneralUtils.DefaultInstallationDirectoriesPath, defaultInstallDirsJson);
        }

        private async Task<string?> ChooseFolder(string desc)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = desc,
                AllowMultiple = false,
            });
            if (folders.Count == 0) return null;
            return folders[0].TryGetLocalPath();
        }

        private string? SelectedExtensionName => ExtensionComboBox.SelectedItem as string;

        private CExtensionLibraryItem? GetSelectedExt()
        {
            var name = SelectedExtensionName;
            if (name == null) return null;
            return _lib.InstalledList.Values.FirstOrDefault(x => x.Name == name);
        }

        private async void BrowseBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedExtensionName == null)
            {
                await CDialogs.ShowMessageAsync("Please select a game before choosing an installation directory", this);
                return;
            }
            var selFolder = await ChooseFolder("Select the folder you want to install your mods to");
            if (selFolder == null) return;
            ModInstallPathTextBox.Text = selFolder;
        }

        private async void AddModBtn_Click(object? sender, RoutedEventArgs e)
        {
            const string WRONG_STRUCT_MSG = "YWML project configuration exists (ywml.json), but is not structured correctly: ";

            var selectedPath = await ChooseFolder("Select the mod folder you want to add");
            if (selectedPath == null) return;

            var ywmlConfigPath = Path.Combine(selectedPath, "ywml.json");
            if (!File.Exists(ywmlConfigPath))
            {
                await CDialogs.ShowMessageAsync("Invalid YWML project: make sure you have a project configuration file (ywml.json)", this);
                return;
            }

            CYwmlProject? ywmlProject;
            try
            {
                ywmlProject = JsonConvert.DeserializeObject<CYwmlProject>(File.ReadAllText(ywmlConfigPath));
            }
            catch
            {
                await CDialogs.ShowMessageAsync(WRONG_STRUCT_MSG + "Wrong json format", this);
                return;
            }
            if (ywmlProject == null)
            {
                await CDialogs.ShowMessageAsync(WRONG_STRUCT_MSG + "Wrong properties", this);
                return;
            }

            var modItem = $"{ywmlProject.Name}";
            _modNameToPath[modItem] = selectedPath;
            Mods.Add(new CModListEntry
            {
                Name = modItem,
                Details = $"{ywmlProject.Author}, {ywmlProject.Version}",
            });
        }

        private void RemoveSelectedModBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (ModsListBox.SelectedItem is CModListEntry entry) Mods.Remove(entry);
        }

        private void ExtensionComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var name = SelectedExtensionName;
            if (name == null || !_nameToId.TryGetValue(name, out var id)) return;

            ModInstallPathTextBox.Text = _defaultInstallDirs.TryGetValue(id, out var dir) ? dir : "";
        }

        private async void InstallBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (Mods.Count == 0)
            {
                await CDialogs.ShowMessageAsync("Please add at least one mod to the list to begin patching.", this);
                return;
            }
            if (SelectedExtensionName == null)
            {
                await CDialogs.ShowMessageAsync("Please select a target game.", this);
                return;
            }

            //check if the mod path contains the title ID to warn the user it may be wrong
            var selectedExtension = GetSelectedExt()!;
            if (!(ModInstallPathTextBox.Text ?? string.Empty).Contains(selectedExtension.TitleId))
            {
                var proceed = await CDialogs.ShowYesNoAsync(
                    "Your selected mod installation directory DOES NOT contain your selected game's title ID.\nThis likely means this folder is NOT the correct mod installation directory. \n\nIf you are aware of this and know what you are doing, Continue. Else, Fix it.\n\nWould you like to continue?",
                    "YWML",
                    this);

                if (!proceed) return;
            }

            var installPath = ModInstallPathTextBox.Text ?? string.Empty;
            var extId = _nameToId[SelectedExtensionName];
            var faToLoad = Path.Combine(CGeneralUtils.ExtensionInstallDirectory, extId, "patchable.fa");

            try
            {
                var result = CLoader.ModifyFA(Mods.Select(m => m.Name).ToList(), _modNameToPath, faToLoad);
                var rawFiles = result.RawFiles;
                var modifiedFA = result.Archive.Save();
                var outputFaPath = Path.Combine(installPath, _lib.InstalledList[extId].FAName);
                string? dir = Path.GetDirectoryName(outputFaPath);

                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllBytes(outputFaPath, modifiedFA);
                foreach (var file in rawFiles)
                {
                    var baseDirectory = file.Value;
                    var filePath = file.Key;
                    var outputPath = Path.Combine(installPath, Path.GetRelativePath(baseDirectory, filePath));
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                    File.Copy(filePath, outputPath, true);
                }
                result.Archive.BaseStream.Close();
                await CDialogs.ShowMessageAsync("Loaded all mods. Enjoy your game!", this);
            }
            catch (DirectoryNotFoundException ex)
            {
                await CDialogs.ShowMessageAsync(ex.Message, this);
            }
        }

        private void MoveUpSelectedModBtn_Click(object? sender, RoutedEventArgs e) => MoveSelectedNodePosition(true);
        private void MoveDownSelectedModBtn_Click(object? sender, RoutedEventArgs e) => MoveSelectedNodePosition(false);

        private void MoveSelectedNodePosition(bool isMoveUp)
        {
            if (ModsListBox.SelectedItem is not CModListEntry entry) return;

            int index = Mods.IndexOf(entry);
            int newIndex = isMoveUp ? index - 1 : index + 1;
            if (newIndex < 0 || newIndex >= Mods.Count) return;

            Mods.Move(index, newIndex);
            ModsListBox.SelectedItem = entry;
        }

        private async void GenModDirBtn_Click(object? sender, RoutedEventArgs e)
        {
            string modInstallDir;
            if (SelectedExtensionName == null)
            {
                await CDialogs.ShowMessageAsync("You must select a target game to generate an installation dir", this);
                return;
            }

            var titleId = GetSelectedExt()!.TitleId;
            var platform = PlatComboBox.SelectedItem?.ToString() ?? string.Empty;

            if (platform != nameof(SPlatform.Modded3DS))
            {
                modInstallDir = CPlatformUtils.EmulatorModDirectory(platform, titleId);
            }
            else
            {
                await CDialogs.ShowMessageAsync(CPlatformUtils.MicroSdPrompt, this);
                var selFolder = await ChooseFolder("Please choose your 3DS microSD card's drive");
                if (selFolder != null)
                {
                    //the last char removal is to remove the separator at the end of the drive path (D:\ or /Volumes/3DS/)
                    selFolder = selFolder.TrimEnd('\\', '/');
                    modInstallDir = $"{selFolder}/luma/titles/{titleId}";
                }
                else
                {
                    await CDialogs.ShowMessageAsync("Operation was cancelled.", this);
                    return;
                }
            }
            ModInstallPathTextBox.Text = modInstallDir + "/romfs";
        }

        private void ModInstallPathTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            var name = SelectedExtensionName;
            if (name == null || !_nameToId.TryGetValue(name, out var id)) return;
            _defaultInstallDirs[id] = ModInstallPathTextBox.Text ?? string.Empty;
        }
    }
}
