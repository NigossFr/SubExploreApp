using System.Diagnostics;

namespace SubExplore.Utilities
{
    public static class ColorDebugHelper
    {
        public static void LogAllResourceDictionaries()
        {
            Debug.WriteLine("=== COMPREHENSIVE COLOR DEBUG ANALYSIS ===");
            
            try
            {
                var app = Application.Current;
                if (app?.Resources == null)
                {
                    Debug.WriteLine("ERROR: Application.Current.Resources is null");
                    return;
                }

                Debug.WriteLine($"Main Application Resources Count: {app.Resources.Count}");
                
                // Check main application resources
                LogResourceDictionary("Main App Resources", app.Resources);
                
                // Check merged dictionaries
                Debug.WriteLine($"\nMerged Dictionaries Count: {app.Resources.MergedDictionaries.Count}");
                int i = 0;
                foreach (var dict in app.Resources.MergedDictionaries)
                {
                    LogResourceDictionary($"Merged Dictionary {i}", dict);
                    i++;
                }

                // Attempt direct color resolution
                Debug.WriteLine("\n=== DIRECT COLOR RESOLUTION TEST ===");
                TestColorResolution("Warning", "#FF9F1C");
                TestColorResolution("SandyBeige", "#F9DCC4");
                TestColorResolution("Accent", "#48CAE4");
                TestColorResolution("CoralRed", "#FF4D6D");
                
                // Check for potential color conflicts
                Debug.WriteLine("\n=== POTENTIAL COLOR CONFLICTS ===");
                CheckForColorConflicts();
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR in LogAllResourceDictionaries: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private static void LogResourceDictionary(string name, ResourceDictionary dictionary)
        {
            Debug.WriteLine($"\n--- {name} ---");
            Debug.WriteLine($"Resource count: {dictionary.Count}");
            
            foreach (var kvp in dictionary)
            {
                if (kvp.Value is Color color)
                {
                    var hex = ColorToHex(color);
                    Debug.WriteLine($"  {kvp.Key}: {color} ({hex})");
                }
                else if (kvp.Key.ToString().ToLower().Contains("color"))
                {
                    Debug.WriteLine($"  {kvp.Key}: {kvp.Value} (Type: {kvp.Value?.GetType().Name})");
                }
            }
        }
        
        private static void TestColorResolution(string resourceKey, string expectedHex)
        {
            try
            {
                if (Application.Current.Resources.TryGetValue(resourceKey, out var resource))
                {
                    if (resource is Color color)
                    {
                        var actualHex = ColorToHex(color);
                        var match = string.Equals(actualHex, expectedHex, StringComparison.OrdinalIgnoreCase);
                        Debug.WriteLine($"{resourceKey}: {actualHex} (Expected: {expectedHex}) - {(match ? "✅ MATCH" : "❌ MISMATCH")}");
                    }
                    else
                    {
                        Debug.WriteLine($"{resourceKey}: Found but not a Color (Type: {resource.GetType().Name})");
                    }
                }
                else
                {
                    Debug.WriteLine($"{resourceKey}: ❌ NOT FOUND");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{resourceKey}: ERROR - {ex.Message}");
            }
        }
        
        private static void CheckForColorConflicts()
        {
            var allResources = new Dictionary<string, object>();
            
            // Collect all resources from main and merged dictionaries
            foreach (var kvp in Application.Current.Resources)
            {
                allResources[kvp.Key.ToString()] = kvp.Value;
            }
            
            foreach (var dict in Application.Current.Resources.MergedDictionaries)
            {
                foreach (var kvp in dict)
                {
                    var key = kvp.Key.ToString();
                    if (allResources.ContainsKey(key))
                    {
                        Debug.WriteLine($"CONFLICT: Key '{key}' found in multiple dictionaries");
                        Debug.WriteLine($"  First value: {allResources[key]}");
                        Debug.WriteLine($"  Second value: {kvp.Value}");
                    }
                    allResources[key] = kvp.Value;
                }
            }
            
            // Look specifically for our target colors
            var targetKeys = new[] { "Warning", "SandyBeige", "Accent", "CoralRed" };
            foreach (var key in targetKeys)
            {
                var matches = allResources.Where(kvp => 
                    kvp.Key.Contains(key, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                    
                if (matches.Count > 1)
                {
                    Debug.WriteLine($"Multiple matches for '{key}':");
                    foreach (var match in matches)
                    {
                        Debug.WriteLine($"  {match.Key}: {match.Value}");
                    }
                }
            }
        }
        
        private static string ColorToHex(Color color)
        {
            var red = (int)(color.Red * 255);
            var green = (int)(color.Green * 255);
            var blue = (int)(color.Blue * 255);
            var alpha = (int)(color.Alpha * 255);
            
            if (alpha == 255)
                return $"#{red:X2}{green:X2}{blue:X2}";
            else
                return $"#{alpha:X2}{red:X2}{green:X2}{blue:X2}";
        }
        
        public static void VerifyColorsXamlFile()
        {
            Debug.WriteLine("=== COLORS.XAML VERIFICATION ===");
            Debug.WriteLine("Expected colors from Colors.xaml:");
            Debug.WriteLine("Warning: #FF9F1C (Orange corail)");
            Debug.WriteLine("SandyBeige: #F9DCC4 (Beige sable)"); 
            Debug.WriteLine("Accent: #48CAE4 (Bleu clair)");
            Debug.WriteLine("CoralRed: #FF4D6D (Rouge corail vif)");
            Debug.WriteLine("\nIf you see different colors, there may be:");
            Debug.WriteLine("1. Multiple resource dictionaries overriding colors");
            Debug.WriteLine("2. Platform-specific resource dictionary issues");
            Debug.WriteLine("3. Build/cache issues not properly cleared");
            Debug.WriteLine("4. Resource dictionary merge order problems");
        }
    }
}