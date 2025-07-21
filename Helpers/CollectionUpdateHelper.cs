using System.Collections.ObjectModel;

namespace SubExplore.Helpers
{
    /// <summary>
    /// Helper class for thread-safe collection updates to prevent UI flicker and race conditions
    /// </summary>
    public static class CollectionUpdateHelper
    {
        /// <summary>
        /// Atomically updates an ObservableCollection on the main thread
        /// This prevents the Clear/Add race condition pattern that causes UI flicker
        /// </summary>
        public static async Task UpdateCollectionAsync<T>(
            ObservableCollection<T> targetCollection,
            IEnumerable<T> newItems,
            Action<ObservableCollection<T>> setterAction)
        {
            var itemsList = newItems?.ToList() ?? new List<T>();
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var newCollection = new ObservableCollection<T>(itemsList);
                setterAction(newCollection);
            });
        }

        /// <summary>
        /// Synchronously updates an ObservableCollection on the main thread
        /// </summary>
        public static void UpdateCollection<T>(
            ObservableCollection<T> targetCollection,
            IEnumerable<T> newItems,
            Action<ObservableCollection<T>> setterAction)
        {
            var itemsList = newItems?.ToList() ?? new List<T>();
            
            if (Application.Current?.Dispatcher?.IsDispatchRequired == true)
            {
                Application.Current.Dispatcher.Dispatch(() =>
                {
                    var newCollection = new ObservableCollection<T>(itemsList);
                    setterAction(newCollection);
                });
            }
            else
            {
                var newCollection = new ObservableCollection<T>(itemsList);
                setterAction(newCollection);
            }
        }

        /// <summary>
        /// Validates that we're on the main thread for UI operations
        /// </summary>
        public static void EnsureMainThread()
        {
            if (Application.Current?.Dispatcher?.IsDispatchRequired == true)
            {
                throw new InvalidOperationException("This operation must be called on the main thread");
            }
        }

        /// <summary>
        /// Safely executes an action on the main thread
        /// </summary>
        public static void ExecuteOnMainThread(Action action)
        {
            if (Application.Current?.Dispatcher?.IsDispatchRequired == true)
            {
                Application.Current.Dispatcher.Dispatch(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Safely executes an async action on the main thread
        /// </summary>
        public static async Task ExecuteOnMainThreadAsync(Func<Task> asyncAction)
        {
            if (MainThread.IsMainThread)
            {
                await asyncAction();
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(asyncAction);
            }
        }
    }
}