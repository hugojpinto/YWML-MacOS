using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YWML.Src.Views
{
    /// <summary>
    /// One node of the extension library tree (category or extension), replacing WinForms TreeNode.
    /// </summary>
    public class CExtensionTreeNode : INotifyPropertyChanged
    {
        private string _text = string.Empty;

        public CExtensionTreeNode(string text, bool isCategory)
        {
            _text = text;
            IsCategory = isCategory;
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text == value) return;
                _text = value;
                OnPropertyChanged();
            }
        }

        public bool IsCategory { get; }

        public ObservableCollection<CExtensionTreeNode> Children { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
