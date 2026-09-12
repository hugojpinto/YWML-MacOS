using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YWML.Src.Views
{
    /// <summary>
    /// One row of the loader's mod list (the WinForms TreeNode + ToolTipText equivalent).
    /// </summary>
    public class CModListEntry : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _details = string.Empty;

        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        /// <summary>Author and version, shown as a tooltip just like the original.</summary>
        public string Details
        {
            get => _details;
            set => Set(ref _details, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set(ref string field, string value, [CallerMemberName] string? name = null)
        {
            if (field == value) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
