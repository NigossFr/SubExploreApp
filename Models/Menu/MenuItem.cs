using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SubExplore.Models.Menu
{
    public partial class MenuItem : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _icon = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private ICommand _command;

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private bool _isVisible = true;

        [ObservableProperty]
        private int _badgeCount;

        [ObservableProperty]
        private string _badgeText = string.Empty;

        [ObservableProperty]
        private bool _hasBadge;

        [ObservableProperty]
        private string _route = string.Empty;

        public MenuItem()
        {
        }

        public MenuItem(string title, string icon, string description, ICommand command)
        {
            Title = title;
            Icon = icon;
            Description = description;
            Command = command;
        }

        partial void OnBadgeCountChanged(int value)
        {
            HasBadge = value > 0;
            BadgeText = value > 99 ? "99+" : value.ToString();
        }
    }
}