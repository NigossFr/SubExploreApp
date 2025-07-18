using System.Collections.ObjectModel;
using SubExplore.Models.Menu;
using SubExplore.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace SubExplore.Services.Implementations
{
    public class MenuService : IMenuService
    {
        private readonly ILogger<MenuService> _logger;
        private readonly ObservableCollection<MenuSection> _menuSections;

        public MenuService(ILogger<MenuService> logger)
        {
            _logger = logger;
            _menuSections = new ObservableCollection<MenuSection>();
        }

        public ObservableCollection<MenuSection> GetMenuSections()
        {
            return _menuSections;
        }

        public MenuSection GetMenuSection(string title)
        {
            return _menuSections.FirstOrDefault(s => s.Title == title);
        }

        public void AddMenuSection(MenuSection section)
        {
            if (section == null)
            {
                _logger.LogWarning("Attempted to add null menu section");
                return;
            }

            var existingSection = GetMenuSection(section.Title);
            if (existingSection != null)
            {
                _logger.LogWarning($"Menu section '{section.Title}' already exists");
                return;
            }

            _menuSections.Add(section);
            _logger.LogInformation($"Added menu section: {section.Title}");
        }

        public void RemoveMenuSection(string title)
        {
            var section = GetMenuSection(title);
            if (section != null)
            {
                _menuSections.Remove(section);
                _logger.LogInformation($"Removed menu section: {title}");
            }
            else
            {
                _logger.LogWarning($"Menu section '{title}' not found for removal");
            }
        }

        public void AddMenuItem(string sectionTitle, SubExplore.Models.Menu.MenuItem item)
        {
            var section = GetMenuSection(sectionTitle);
            if (section == null)
            {
                _logger.LogWarning($"Menu section '{sectionTitle}' not found for adding item");
                return;
            }

            if (item == null)
            {
                _logger.LogWarning("Attempted to add null menu item");
                return;
            }

            section.AddItem(item);
            _logger.LogInformation($"Added menu item '{item.Title}' to section '{sectionTitle}'");
        }

        public void RemoveMenuItem(string sectionTitle, string itemTitle)
        {
            var section = GetMenuSection(sectionTitle);
            if (section == null)
            {
                _logger.LogWarning($"Menu section '{sectionTitle}' not found for removing item");
                return;
            }

            var item = section.Items.FirstOrDefault(i => i.Title == itemTitle);
            if (item != null)
            {
                section.RemoveItem(item);
                _logger.LogInformation($"Removed menu item '{itemTitle}' from section '{sectionTitle}'");
            }
            else
            {
                _logger.LogWarning($"Menu item '{itemTitle}' not found in section '{sectionTitle}'");
            }
        }

        public void UpdateMenuItemBadge(string sectionTitle, string itemTitle, int badgeCount)
        {
            var section = GetMenuSection(sectionTitle);
            if (section == null)
            {
                _logger.LogWarning($"Menu section '{sectionTitle}' not found for updating badge");
                return;
            }

            var item = section.Items.FirstOrDefault(i => i.Title == itemTitle);
            if (item != null)
            {
                item.BadgeCount = badgeCount;
                _logger.LogInformation($"Updated badge count for '{itemTitle}' to {badgeCount}");
            }
            else
            {
                _logger.LogWarning($"Menu item '{itemTitle}' not found in section '{sectionTitle}'");
            }
        }

        public void SetMenuItemEnabled(string sectionTitle, string itemTitle, bool isEnabled)
        {
            var section = GetMenuSection(sectionTitle);
            if (section == null)
            {
                _logger.LogWarning($"Menu section '{sectionTitle}' not found for enabling/disabling item");
                return;
            }

            var item = section.Items.FirstOrDefault(i => i.Title == itemTitle);
            if (item != null)
            {
                item.IsEnabled = isEnabled;
                _logger.LogInformation($"Set menu item '{itemTitle}' enabled: {isEnabled}");
            }
            else
            {
                _logger.LogWarning($"Menu item '{itemTitle}' not found in section '{sectionTitle}'");
            }
        }

        public void SetMenuItemVisible(string sectionTitle, string itemTitle, bool isVisible)
        {
            var section = GetMenuSection(sectionTitle);
            if (section == null)
            {
                _logger.LogWarning($"Menu section '{sectionTitle}' not found for showing/hiding item");
                return;
            }

            var item = section.Items.FirstOrDefault(i => i.Title == itemTitle);
            if (item != null)
            {
                item.IsVisible = isVisible;
                _logger.LogInformation($"Set menu item '{itemTitle}' visible: {isVisible}");
            }
            else
            {
                _logger.LogWarning($"Menu item '{itemTitle}' not found in section '{sectionTitle}'");
            }
        }

        public void InitializeDefaultMenu()
        {
            ClearMenu();
            _logger.LogInformation("Initializing default menu");
            
            // This method is typically called by the MenuViewModel
            // The actual menu initialization is handled there
        }

        public void ClearMenu()
        {
            _menuSections.Clear();
            _logger.LogInformation("Cleared all menu sections");
        }
    }
}