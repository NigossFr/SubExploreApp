using Microsoft.Maui.Controls;

namespace SubExplore.Helpers
{
    /// <summary>
    /// Provides smooth navigation transitions and animations
    /// </summary>
    public static class NavigationTransitions
    {
        /// <summary>
        /// Apply slide transition to a page during navigation
        /// </summary>
        public static async Task ApplySlideTransition(Page page, bool isEntering = true, int duration = 300)
        {
            try
            {
                if (page == null) return;

                var startTranslationX = isEntering ? (int)page.Width : -(int)page.Width;
                var endTranslationX = 0;

                if (!isEntering)
                {
                    startTranslationX = 0;
                    endTranslationX = (int)page.Width;
                }

                page.TranslationX = startTranslationX;
                page.Opacity = isEntering ? 0 : 1;

                await Task.WhenAll(
                    page.TranslateTo(endTranslationX, 0, (uint)duration, Easing.CubicOut),
                    page.FadeTo(isEntering ? 1 : 0, (uint)duration, Easing.CubicOut)
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTransitions] ApplySlideTransition error: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply fade transition to a page during navigation
        /// </summary>
        public static async Task ApplyFadeTransition(Page page, bool isEntering = true, int duration = 250)
        {
            try
            {
                if (page == null) return;

                var startOpacity = isEntering ? 0 : 1;
                var endOpacity = isEntering ? 1 : 0;

                page.Opacity = startOpacity;
                await page.FadeTo(endOpacity, (uint)duration, Easing.CubicInOut);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTransitions] ApplyFadeTransition error: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply scale transition to a page during navigation
        /// </summary>
        public static async Task ApplyScaleTransition(Page page, bool isEntering = true, int duration = 300)
        {
            try
            {
                if (page == null) return;

                var startScale = isEntering ? 0.8 : 1.0;
                var endScale = isEntering ? 1.0 : 0.8;

                page.Scale = startScale;
                page.Opacity = isEntering ? 0 : 1;

                await Task.WhenAll(
                    page.ScaleTo(endScale, (uint)duration, Easing.CubicOut),
                    page.FadeTo(isEntering ? 1 : 0, (uint)duration, Easing.CubicOut)
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTransitions] ApplyScaleTransition error: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply bottom-up transition for modal pages
        /// </summary>
        public static async Task ApplyBottomUpTransition(Page page, bool isEntering = true, int duration = 350)
        {
            try
            {
                if (page == null) return;

                var startTranslationY = isEntering ? (int)page.Height : 0;
                var endTranslationY = isEntering ? 0 : (int)page.Height;

                page.TranslationY = startTranslationY;
                page.Opacity = isEntering ? 0.9 : 1;

                await Task.WhenAll(
                    page.TranslateTo(0, endTranslationY, (uint)duration, Easing.CubicOut),
                    page.FadeTo(isEntering ? 1 : 0, (uint)duration, Easing.CubicOut)
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTransitions] ApplyBottomUpTransition error: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset page transforms after transition
        /// </summary>
        public static void ResetPageTransforms(Page page)
        {
            try
            {
                if (page == null) return;

                page.TranslationX = 0;
                page.TranslationY = 0;
                page.Scale = 1.0;
                page.Opacity = 1.0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationTransitions] ResetPageTransforms error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get transition type based on navigation context
        /// </summary>
        public static TransitionType GetTransitionForNavigation(string fromRoute, string toRoute)
        {
            // Modal pages (Add Spot, Spot Details) use bottom-up
            if (toRoute.Contains("addspot") || toRoute.Contains("spotdetails"))
            {
                return TransitionType.BottomUp;
            }

            // Admin pages use scale transition
            if (toRoute.Contains("spotvalidation") || toRoute.Contains("admin"))
            {
                return TransitionType.Scale;
            }

            // Profile and settings use fade
            if (toRoute.Contains("profile") || toRoute.Contains("preferences") || toRoute.Contains("stats"))
            {
                return TransitionType.Fade;
            }

            // Default slide transition
            return TransitionType.Slide;
        }
    }

    public enum TransitionType
    {
        Slide,
        Fade,
        Scale,
        BottomUp
    }
}