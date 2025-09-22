using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services;

/// <summary>
/// Text component management service providing text component creation and manipulation.
/// </summary>
[Service("Text Component Service", "1.0.0", "Text component creation and management for console UI",
    Categories = new[] { "UI", "Text", "Components" })]
public sealed class TextComponentService : BaseUIService, ITextComponentCapability
{
    private readonly List<IUIComponent> _rootComponents = new();
    private readonly object _componentsLock = new();

    public TextComponentService(ILogger<TextComponentService> logger)
        : base(logger) { }

    #region BaseUIService Implementation

    public override ITextComponentCapability TextComponents => this;

    public override bool SupportsColors => 
        Environment.GetEnvironmentVariable("NO_COLOR") == null && 
        !Console.IsOutputRedirected;

    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing text component service");
        return Task.CompletedTask;
    }

    public override Task RenderAsync(CancellationToken cancellationToken = default)
    {
        // This service doesn't render directly - it manages components
        return Task.CompletedTask;
    }

    public override Task ClearScreenAsync(CancellationToken cancellationToken = default)
    {
        Console.Clear();
        return Task.CompletedTask;
    }

    public override Task AddComponentAsync(IUIComponent component, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        lock (_componentsLock)
        {
            _rootComponents.Add(component);
        }
        
        return Task.CompletedTask;
    }

    public override Task RemoveComponentAsync(IUIComponent component, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        lock (_componentsLock)
        {
            _rootComponents.Remove(component);
        }
        
        return Task.CompletedTask;
    }

    public override Task<Size> GetConsoleSize(CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(new Size(Console.WindowWidth, Console.WindowHeight));
        }
        catch (IOException)
        {
            return Task.FromResult(new Size(80, 25));
        }
    }

    #endregion

    #region ITextComponentCapability Implementation

    public Task<ITextComponent> CreateTextComponentAsync(string id, string text, Position position, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        var textComponent = new TextComponent(id, text, position);
        _logger.LogDebug("Created text component {ComponentId} at {Position}", id, position);
        
        return Task.FromResult<ITextComponent>(textComponent);
    }

    public Task UpdateTextAsync(ITextComponent component, string newText, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (component is TextComponent textComp)
        {
            textComp.Text = newText;
            textComp.Invalidate();
            _logger.LogDebug("Updated text for component {ComponentId}", component.Id);
        }
        
        return Task.CompletedTask;
    }

    public Task SetColorsAsync(ITextComponent component, ConsoleColor colors, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (component is TextComponent textComp)
        {
            textComp.Colors = colors;
            textComp.Invalidate();
            _logger.LogDebug("Updated colors for component {ComponentId}", component.Id);
        }
        
        return Task.CompletedTask;
    }

    #endregion
}

/// <summary>
/// Basic implementation of a text UI component.
/// </summary>
internal sealed class TextComponent : ITextComponent
{
    private readonly List<IUIComponent> _children = new();
    private Position _position;
    private Size _size;
    private Visibility _visibility = Visibility.Visible;
    private string _text;
    private TextAlignment _textAlignment = TextAlignment.Left;
    private VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
    private ConsoleColor _colors = new(System.ConsoleColor.White, System.ConsoleColor.Black);

    public TextComponent(string id, string text, Position position)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        _text = text ?? string.Empty;
        _position = position;
        _size = new Size(text?.Length ?? 0, 1); // Single line by default
    }

    #region IUIComponent Implementation

    public string Id { get; }

    public Position Position
    {
        get => _position;
        set
        {
            _position = value;
            Invalidate();
        }
    }

    public Size Size
    {
        get => _size;
        set
        {
            _size = value;
            Invalidate();
        }
    }

    public Visibility Visibility
    {
        get => _visibility;
        set
        {
            _visibility = value;
            Invalidate();
        }
    }

    public UIBounds Bounds => new(_position, _size);

    public IUIComponent? Parent { get; private set; }

    public IReadOnlyList<IUIComponent> Children => _children.AsReadOnly();

    public event EventHandler? Invalidated;

    public void AddChild(IUIComponent child)
    {
        if (child is TextComponent textChild)
        {
            textChild.Parent = this;
            _children.Add(child);
        }
    }

    public bool RemoveChild(IUIComponent child)
    {
        if (child is TextComponent textChild && _children.Remove(child))
        {
            textChild.Parent = null;
            return true;
        }
        return false;
    }

    public void Invalidate()
    {
        Invalidated?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region ITextComponent Implementation

    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            // Auto-resize based on text length if size hasn't been explicitly set
            if (_size.Width < _text.Length)
            {
                _size = new Size(_text.Length, _size.Height);
            }
            Invalidate();
        }
    }

    public TextAlignment TextAlignment
    {
        get => _textAlignment;
        set
        {
            _textAlignment = value;
            Invalidate();
        }
    }

    public VerticalAlignment VerticalAlignment
    {
        get => _verticalAlignment;
        set
        {
            _verticalAlignment = value;
            Invalidate();
        }
    }

    public ConsoleColor Colors
    {
        get => _colors;
        set
        {
            _colors = value;
            Invalidate();
        }
    }

    #endregion

    #region ICapabilityProvider Implementation

    public TCapability? GetCapability<TCapability>() where TCapability : class, ICapabilityProvider
    {
        return this as TCapability;
    }

    public IEnumerable<Type> GetSupportedCapabilities()
    {
        yield return typeof(ITextComponent);
    }

    #endregion
}