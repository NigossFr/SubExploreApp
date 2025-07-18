using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SubExplore.Models.Menu
{
    public partial class MenuSection : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private ObservableCollection<MenuItem> _items = new();

        [ObservableProperty]
        private bool _isVisible = true;

        [ObservableProperty]
        private bool _isExpanded = true;

        public MenuSection()
        {
            Items = new ObservableCollection<MenuItem>();
        }

        public MenuSection(string title)
        {
            Title = title;
            Items = new ObservableCollection<MenuItem>();
        }

        public void AddItem(MenuItem item)
        {
            Items.Add(item);
        }

        public void RemoveItem(MenuItem item)
        {
            Items.Remove(item);
        }

        public void ClearItems()
        {
            Items.Clear();
        }
    }
}