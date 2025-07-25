﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubExplore.Services.Interfaces
{
    public interface IDialogService
    {
        /// <summary>
        /// Affiche une alerte avec un seul bouton.
        /// </summary>
        Task ShowAlertAsync(string title, string message, string buttonText);

        /// <summary>
        /// Affiche une alerte avec un choix binaire (OK/Annuler).
        /// </summary>
        /// <returns>True si l'utilisateur a choisi OK, False sinon</returns>
        Task<bool> ShowConfirmationAsync(string title, string message, string okText, string cancelText);

        /// <summary>
        /// Affiche une alerte permettant la saisie de texte.
        /// </summary>
        /// <returns>Le texte saisi par l'utilisateur ou null si annulé</returns>
        Task<string> ShowPromptAsync(string title, string message, string okText, string cancelText, string placeholder = "", string initialValue = "");

        /// <summary>
        /// Affiche un toast (notification brève).
        /// </summary>
        Task ShowToastAsync(string message, int durationInSeconds = 2);

        /// <summary>
        /// Affiche un indicateur de chargement.
        /// </summary>
        /// <returns>Un IDisposable qui cache l'indicateur lors du Dispose</returns>
        Task<IDisposable> ShowLoadingAsync(string message = "Chargement...");

        /// <summary>
        /// Affiche une feuille d'action avec plusieurs options.
        /// </summary>
        /// <param name="title">Titre de la feuille d'action</param>
        /// <param name="cancel">Texte du bouton d'annulation</param>
        /// <param name="destruction">Texte du bouton de destruction (optionnel)</param>
        /// <param name="buttons">Liste des options à afficher</param>
        /// <returns>Le texte de l'option sélectionnée ou null si annulé</returns>
        Task<string> ShowActionSheetAsync(string title, string cancel, string destruction, params string[] buttons);
    }
}
