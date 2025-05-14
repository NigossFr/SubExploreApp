using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.Services.Interfaces;
using System.Text.Json;

namespace SubExplore.Services.Implementations
{
    public class SettingsService : ISettingsService
    {
        public void Set<T>(string key, T value)
        {
            if (value == null)
            {
                Remove(key);
                return;
            }

            // Pour les types primitifs, utiliser directement Preferences
            if (typeof(T) == typeof(string) ||
                typeof(T) == typeof(int) ||
                typeof(T) == typeof(double) ||
                typeof(T) == typeof(float) ||
                typeof(T) == typeof(long) ||
                typeof(T) == typeof(bool) ||
                typeof(T) == typeof(DateTime))
            {
                Preferences.Set(key, value.ToString());
                return;
            }

            // Pour les objets complexes, sérialiser en JSON
            var json = JsonSerializer.Serialize(value);
            Preferences.Set(key, json);
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            if (!Preferences.ContainsKey(key))
                return defaultValue;

            var value = Preferences.Get(key, string.Empty);

            // Pour les types primitifs
            if (typeof(T) == typeof(string))
                return (T)(object)value;

            if (typeof(T) == typeof(int))
                return (T)(object)int.Parse(value);

            if (typeof(T) == typeof(double))
                return (T)(object)double.Parse(value);

            if (typeof(T) == typeof(float))
                return (T)(object)float.Parse(value);

            if (typeof(T) == typeof(long))
                return (T)(object)long.Parse(value);

            if (typeof(T) == typeof(bool))
                return (T)(object)bool.Parse(value);

            if (typeof(T) == typeof(DateTime))
                return (T)(object)DateTime.Parse(value);

            // Pour les objets complexes, désérialiser depuis JSON
            try
            {
                return JsonSerializer.Deserialize<T>(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        public bool Contains(string key)
        {
            return Preferences.ContainsKey(key);
        }

        public void Remove(string key)
        {
            if (Preferences.ContainsKey(key))
                Preferences.Remove(key);
        }

        public void Clear()
        {
            Preferences.Clear();
        }
    }
}
