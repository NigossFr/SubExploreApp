using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubExplore.ViewModels.Base;

namespace SubExplore.Services.Interfaces
{
    public interface INavigationService
    {
        /// <summary>
        /// Navigue vers une page spécifique en instanciant son ViewModel.
        /// </summary>
        /// <typeparam name="TViewModel">Type du ViewModel cible</typeparam>
        /// <param name="parameter">Paramètre optionnel à passer au ViewModel</param>
        Task NavigateToAsync<TViewModel>(object parameter = null);

        /// <summary>
        /// Navigue vers une page modale.
        /// </summary>
        /// <typeparam name="TViewModel">Type du ViewModel cible</typeparam>
        /// <param name="parameter">Paramètre optionnel à passer au ViewModel</param>
        Task NavigateToModalAsync<TViewModel>(object parameter = null);

        /// <summary>
        /// Revient à la page précédente.
        /// </summary>
        Task GoBackAsync();

        /// <summary>
        /// Ferme une page modale.
        /// </summary>
        Task CloseModalAsync();

        /// <summary>
        /// Initialise le service de navigation avec la page d'accueil.
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel de la page d'accueil</typeparam>
        Task InitializeAsync<TViewModel>();
    }
}
