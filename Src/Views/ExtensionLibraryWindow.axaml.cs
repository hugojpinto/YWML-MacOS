using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Newtonsoft.Json;
using YWML.Src.ExtensionLibrary;
using YWML.Src.Utils.GeneralUtils;

namespace YWML.Src.Views
{
    public partial class ExtensionLibraryWindow : Window
    {
        private readonly CExtensionLibrary _lib = new();
        private readonly IProgress<string> _status;
        private readonly IProgress<string> _percentage;
        private bool _initialized;

        public ObservableCollection<CExtensionTreeNode> Nodes { get; } = new();

        public ExtensionLibraryWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Progress<T> captures Avalonia's UI SynchronizationContext, so background reports
            // land back on the UI thread (the WinForms Label.Invoke equivalent).
            _status = new Progress<string>(text => StatusLabel.Text = text);
            _percentage = new Progress<string>(text => PercentageLabel.Text = text);

            UninstallBtn.IsEnabled = false;
            InstallBtn.IsEnabled = false;

            Opened += ExtensionLibraryWindow_Opened;
            Closing += ExtensionLibraryWindow_Closing;
        }

        private async void ExtensionLibraryWindow_Opened(object? sender, EventArgs e)
        {
            if (_initialized) return;
            _initialized = true;

            await _lib.LoadInstalledListAsync();
            try
            {
                await _lib.FetchDataAsync();
            }
            catch (HttpRequestException)
            {
                Close();
                return;
            }

            //add category nodes
            var categories = new Dictionary<string, CExtensionTreeNode>();
            foreach (var cat in _lib.ExtensionInfo.ExtensionCategories)
            {
                var node = new CExtensionTreeNode(cat, true);
                categories[cat] = node;
                Nodes.Add(node);
            }
            //add extension nodes to category nodes
            foreach (var ext in _lib.ExtensionInfo.ExtensionList)
            {
                var toAdd = $"{ext.Name} ({ext.FileSize} MiB)";
                if (_lib.InstalledList.ContainsKey(ext.Id))
                {
                    toAdd += " (Installed)";
                }
                foreach (var cat in _lib.ExtensionInfo.ExtensionCategories)
                {
                    if (ext.Name.Contains(cat))
                    {
                        categories[cat].Children.Add(new CExtensionTreeNode(toAdd, false));
                    }
                }
            }
        }

        private void ExtensionLibraryWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            var installedListJson = JsonConvert.SerializeObject(_lib.InstalledList);
            Directory.CreateDirectory(Path.GetDirectoryName(CGeneralUtils.ExtensionInstalledList)!);
            File.WriteAllText(CGeneralUtils.ExtensionInstalledList, installedListJson);
        }

        private CExtensionTreeNode? SelectedNode => ExtensionsTreeView.SelectedItem as CExtensionTreeNode;

        private void SetBusy(bool busy)
        {
            ExitBtn.IsEnabled = !busy;
            InstallBtn.IsEnabled = !busy;
            UninstallBtn.IsEnabled = !busy;
        }

        private async void InstallBtn_Click(object? sender, RoutedEventArgs e)
        {
            var node = SelectedNode;
            if (node == null) return;

            var extension = _lib.ExtensionInfo.ExtensionList.Find(ext => node.Text.Contains(ext.Name));
            if (extension == null) return;

            await extension.InstallAsync(SetBusy, _status, _percentage, _lib.InstalledList);
            node.Text += " (Installed)";
            InstallBtn.IsEnabled = false;
            UninstallBtn.IsEnabled = true;
        }

        private void ExitBtn_Click(object? sender, RoutedEventArgs e) => Close();

        private void ExtensionsTreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var node = SelectedNode;
            //check that selected node is not a category node
            if (node == null || node.IsCategory)
            {
                UninstallBtn.IsEnabled = false;
                InstallBtn.IsEnabled = false;
                return;
            }

            if (node.Text.Contains("Installed"))
            {
                UninstallBtn.IsEnabled = true;
                InstallBtn.IsEnabled = false;
            }
            else
            {
                InstallBtn.IsEnabled = true;
                UninstallBtn.IsEnabled = false;
            }
        }

        private async void UninstallBtn_Click(object? sender, RoutedEventArgs e)
        {
            var node = SelectedNode;
            if (node == null) return;

            PercentageLabel.Text = string.Empty;
            var extension = _lib.ExtensionInfo.ExtensionList.Find(ext => node.Text.Contains(ext.Name));
            if (extension == null) return;

            await extension.UninstallAsync(SetBusy, _status, _lib.InstalledList);
            node.Text = node.Text.Substring(0, node.Text.Length - " (Installed)".Length);
            InstallBtn.IsEnabled = true;
            UninstallBtn.IsEnabled = false;
        }
    }
}
