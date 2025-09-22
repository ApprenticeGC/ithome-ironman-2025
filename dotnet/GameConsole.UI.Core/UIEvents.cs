namespace GameConsole.UI.Core;

/// <summary>
/// Event arguments for UI surface size changes.
/// </summary>
public class SizeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the old size before the change.
    /// </summary>
    public Size OldSize { get; }
    
    /// <summary>
    /// Gets the new size after the change.
    /// </summary>
    public Size NewSize { get; }
    
    /// <summary>
    /// Initializes a new instance of the SizeChangedEventArgs class.
    /// </summary>
    /// <param name="oldSize">The old size.</param>
    /// <param name="newSize">The new size.</param>
    public SizeChangedEventArgs(Size oldSize, Size newSize)
    {
        OldSize = oldSize;
        NewSize = newSize;
    }
}