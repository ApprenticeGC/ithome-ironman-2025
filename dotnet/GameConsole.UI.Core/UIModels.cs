namespace GameConsole.UI.Core;

/// <summary>
/// Represents different types of UI messages.
/// </summary>
public enum MessageType
{
    Info,
    Warning,
    Error,
    Success
}

/// <summary>
/// Represents a UI message to be displayed.
/// </summary>
public class UIMessage
{
    public string Content { get; }
    public MessageType Type { get; }
    public DateTime Timestamp { get; }

    public UIMessage(string content, MessageType type)
    {
        Content = content;
        Type = type;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a menu item in the console UI.
/// </summary>
public class MenuItem
{
    public string Id { get; }
    public string Display { get; }
    public string? Description { get; }
    public bool IsEnabled { get; }

    public MenuItem(string id, string display, string? description = null, bool isEnabled = true)
    {
        Id = id;
        Display = display;
        Description = description;
        IsEnabled = isEnabled;
    }
}

/// <summary>
/// Represents a menu with multiple items.
/// </summary>
public class Menu
{
    public string Title { get; }
    public IReadOnlyList<MenuItem> Items { get; }

    public Menu(string title, IEnumerable<MenuItem> items)
    {
        Title = title;
        Items = items.ToArray();
    }
}