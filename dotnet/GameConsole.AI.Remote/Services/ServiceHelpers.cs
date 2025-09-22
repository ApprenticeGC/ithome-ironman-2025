using System.Runtime.CompilerServices;

namespace GameConsole.AI.Remote.Services;

/// <summary>
/// Helper utilities for the Remote AI services.
/// </summary>
internal static class ServiceHelpers
{
    /// <summary>
    /// Throws ObjectDisposedException if the object has been disposed.
    /// </summary>
    /// <param name="disposed">Whether the object has been disposed.</param>
    /// <param name="objectName">Name of the object for the exception.</param>
    /// <exception cref="ObjectDisposedException">Thrown if disposed is true.</exception>
    public static void ThrowIfDisposed(bool disposed, [CallerMemberName] string objectName = "")
    {
        if (disposed)
        {
            throw new ObjectDisposedException(objectName);
        }
    }
}