namespace SubExplore.Navigation
{
    /// <summary>
    /// Attribute to mark ViewModels with their corresponding Shell routes
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ShellRouteAttribute : Attribute
    {
        public string Route { get; }
        public string? FriendlyName { get; set; }
        public string? Icon { get; set; }
        public bool IsVisible { get; set; } = true;

        public ShellRouteAttribute(string route)
        {
            Route = route ?? throw new ArgumentNullException(nameof(route));
        }
    }

    /// <summary>
    /// Represents a registered shell route with its metadata
    /// </summary>
    public class ShellRouteInfo
    {
        public Type ViewModelType { get; set; } = null!;
        public Type ViewType { get; set; } = null!;
        public string Route { get; set; } = null!;
        public string FriendlyName { get; set; } = null!;
        public string? Icon { get; set; }
        public bool IsVisible { get; set; }
    }
}