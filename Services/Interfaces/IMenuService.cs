using SubExplore.Models.Menu;
using System.Collections.ObjectModel;

namespace SubExplore.Services.Interfaces
{
    public interface IMenuService
    {
        /// <summary>
        /// Get all menu sections
        /// </summary>
        ObservableCollection<MenuSection> GetMenuSections();

        /// <summary>
        /// Get menu section by title
        /// </summary>
        MenuSection GetMenuSection(string title);

        /// <summary>
        /// Add a new menu section
        /// </summary>
        void AddMenuSection(MenuSection section);

        /// <summary>
        /// Remove a menu section
        /// </summary>
        void RemoveMenuSection(string title);

        /// <summary>
        /// Add menu item to a section
        /// </summary>
        void AddMenuItem(string sectionTitle, SubExplore.Models.Menu.MenuItem item);

        /// <summary>
        /// Remove menu item from a section
        /// </summary>
        void RemoveMenuItem(string sectionTitle, string itemTitle);

        /// <summary>
        /// Update menu item badge count
        /// </summary>
        void UpdateMenuItemBadge(string sectionTitle, string itemTitle, int badgeCount);

        /// <summary>
        /// Enable or disable menu item
        /// </summary>
        void SetMenuItemEnabled(string sectionTitle, string itemTitle, bool isEnabled);

        /// <summary>
        /// Show or hide menu item
        /// </summary>
        void SetMenuItemVisible(string sectionTitle, string itemTitle, bool isVisible);

        /// <summary>
        /// Initialize menu with default items
        /// </summary>
        void InitializeDefaultMenu();

        /// <summary>
        /// Clear all menu items
        /// </summary>
        void ClearMenu();
    }
}