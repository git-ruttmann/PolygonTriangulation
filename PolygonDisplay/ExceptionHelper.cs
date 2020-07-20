namespace PolygonDisplay
{
    using System;

    /// <summary>
    /// exception handling helper
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// Determines whether the exception can be swallowed.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>true if the exception is not too severe</returns>
        public static bool CanSwallow(Exception exception)
        {
            if (exception is OutOfMemoryException)
            {
                return false;
            }

            return true;
        }
    }
}
