using System;

namespace SubExplore.Exceptions
{
    /// <summary>
    /// Exception thrown when navigation operations fail
    /// </summary>
    public class NavigationException : Exception
    {
        public NavigationException(string message) : base(message)
        {
        }

        public NavigationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}