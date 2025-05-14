using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    public interface ISettingsService
    {
        /// <summary>
        /// Stocke une valeur dans les paramètres de l'application
        /// </summary>
        void Set<T>(string key, T value);

        /// <summary>
        /// Récupère une valeur depuis les paramètres de l'application
        /// </summary>
        T Get<T>(string key, T defaultValue = default);

        /// <summary>
        /// Vérifie si une clé existe dans les paramètres
        /// </summary>
        bool Contains(string key);

        /// <summary>
        /// Supprime une clé des paramètres
        /// </summary>
        void Remove(string key);

        /// <summary>
        /// Efface tous les paramètres
        /// </summary>
        void Clear();
    }
}
