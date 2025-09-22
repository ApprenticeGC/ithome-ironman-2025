namespace GameConsole.UI.Console;

/// <summary>
/// Implementation of console layout manager for positioning UI components.
/// </summary>
public class ConsoleLayoutManager : IConsoleLayoutManager
{
    private readonly IConsoleUIFramework _framework;
    
    public ConsoleLayoutManager(IConsoleUIFramework framework)
    {
        _framework = framework ?? throw new ArgumentNullException(nameof(framework));
    }
    
    public Rectangle ScreenBounds => new(0, 0, _framework.Width, _framework.Height);
    
    public IReadOnlyDictionary<string, Rectangle> CalculateLayout(IEnumerable<IUIComponent> components)
    {
        var result = new Dictionary<string, Rectangle>();
        var componentList = components.ToList();
        
        if (!componentList.Any())
            return result;
            
        var availableArea = ScreenBounds;
        
        // Simple layout strategy: stack components vertically
        int currentY = 0;
        int remainingHeight = availableArea.Height;
        int componentCount = componentList.Count;
        
        foreach (var component in componentList)
        {
            var desiredBounds = component.DesiredBounds;
            
            // Calculate component height
            int componentHeight;
            if (desiredBounds.Height > 0)
            {
                componentHeight = Math.Min(desiredBounds.Height, remainingHeight);
            }
            else
            {
                // Auto-size: distribute remaining space evenly
                componentHeight = remainingHeight / componentCount;
            }
            
            // Calculate component width
            int componentWidth = desiredBounds.Width > 0 
                ? Math.Min(desiredBounds.Width, availableArea.Width)
                : availableArea.Width;
            
            var bounds = new Rectangle(
                availableArea.X,
                availableArea.Y + currentY,
                componentWidth,
                componentHeight
            );
            
            result[component.Id] = bounds;
            component.SetBounds(bounds);
            
            currentY += componentHeight;
            remainingHeight -= componentHeight;
            componentCount--;
            
            if (remainingHeight <= 0) break;
        }
        
        return result;
    }
    
    public ILayoutRegion CreateRegion(Rectangle bounds)
    {
        return new LayoutRegion(bounds);
    }
}

/// <summary>
/// Implementation of layout region for dividing screen space.
/// </summary>
public class LayoutRegion : ILayoutRegion
{
    public Rectangle Bounds { get; private set; }
    
    public LayoutRegion(Rectangle bounds)
    {
        Bounds = bounds;
    }
    
    public ILayoutRegion[] Subdivide(int divisions, LayoutDirection direction)
    {
        if (divisions <= 1) return new[] { this };
        
        var regions = new ILayoutRegion[divisions];
        
        if (direction == LayoutDirection.Horizontal)
        {
            int widthPerRegion = Bounds.Width / divisions;
            int remainingWidth = Bounds.Width % divisions;
            
            int currentX = Bounds.X;
            for (int i = 0; i < divisions; i++)
            {
                int regionWidth = widthPerRegion + (i < remainingWidth ? 1 : 0);
                regions[i] = new LayoutRegion(new Rectangle(currentX, Bounds.Y, regionWidth, Bounds.Height));
                currentX += regionWidth;
            }
        }
        else // Vertical
        {
            int heightPerRegion = Bounds.Height / divisions;
            int remainingHeight = Bounds.Height % divisions;
            
            int currentY = Bounds.Y;
            for (int i = 0; i < divisions; i++)
            {
                int regionHeight = heightPerRegion + (i < remainingHeight ? 1 : 0);
                regions[i] = new LayoutRegion(new Rectangle(Bounds.X, currentY, Bounds.Width, regionHeight));
                currentY += regionHeight;
            }
        }
        
        return regions;
    }
    
    public ILayoutRegion[] Split(int position, LayoutDirection direction)
    {
        if (direction == LayoutDirection.Horizontal)
        {
            if (position <= 0 || position >= Bounds.Width)
                return new[] { this };
                
            var left = new LayoutRegion(new Rectangle(Bounds.X, Bounds.Y, position, Bounds.Height));
            var right = new LayoutRegion(new Rectangle(Bounds.X + position, Bounds.Y, Bounds.Width - position, Bounds.Height));
            return new[] { left, right };
        }
        else // Vertical
        {
            if (position <= 0 || position >= Bounds.Height)
                return new[] { this };
                
            var top = new LayoutRegion(new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, position));
            var bottom = new LayoutRegion(new Rectangle(Bounds.X, Bounds.Y + position, Bounds.Width, Bounds.Height - position));
            return new[] { top, bottom };
        }
    }
}