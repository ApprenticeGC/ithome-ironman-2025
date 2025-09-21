namespace GameConsole.UI.Core;

/// <summary>
/// Represents a UI component's properties and metadata.
/// </summary>
public record UIComponentDescriptor
{
    /// <summary>
    /// Gets the unique identifier for the component.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the component type name.
    /// </summary>
    public string TypeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name for the component.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the component category.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Gets the supported capabilities for this component.
    /// </summary>
    public UICapabilities SupportedCapabilities { get; init; }

    /// <summary>
    /// Gets the properties available on this component.
    /// </summary>
    public List<UIPropertyDescriptor> Properties { get; init; } = new();

    /// <summary>
    /// Gets the events that this component can raise.
    /// </summary>
    public List<UIEventDescriptor> Events { get; init; } = new();

    /// <summary>
    /// Gets the parent component ID (if any).
    /// </summary>
    public string? ParentId { get; init; }

    /// <summary>
    /// Gets the child component IDs.
    /// </summary>
    public List<string> ChildrenIds { get; init; } = new();
}

/// <summary>
/// Describes a property on a UI component.
/// </summary>
public record UIPropertyDescriptor
{
    /// <summary>
    /// Gets the property name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the property type.
    /// </summary>
    public Type PropertyType { get; init; } = typeof(object);

    /// <summary>
    /// Gets the default value for the property.
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Gets whether the property is required.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets whether the property is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Gets the property description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets validation rules for the property.
    /// </summary>
    public List<UIValidationRule> ValidationRules { get; init; } = new();
}

/// <summary>
/// Describes an event that a UI component can raise.
/// </summary>
public record UIEventDescriptor
{
    /// <summary>
    /// Gets the event name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the event arguments type.
    /// </summary>
    public Type EventArgsType { get; init; } = typeof(EventArgs);

    /// <summary>
    /// Gets the event description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether the event bubbles.
    /// </summary>
    public bool Bubbles { get; init; } = true;

    /// <summary>
    /// Gets whether the event is cancelable.
    /// </summary>
    public bool Cancelable { get; init; } = false;
}

/// <summary>
/// Represents a validation rule for UI properties.
/// </summary>
public record UIValidationRule
{
    /// <summary>
    /// Gets the rule type.
    /// </summary>
    public UIValidationRuleType RuleType { get; init; }

    /// <summary>
    /// Gets the validation parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();

    /// <summary>
    /// Gets the error message when validation fails.
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;
}

/// <summary>
/// Represents styling information for UI components.
/// </summary>
public record UIStyle
{
    /// <summary>
    /// Gets CSS-like style properties.
    /// </summary>
    public Dictionary<string, string> Properties { get; init; } = new();

    /// <summary>
    /// Gets CSS classes to apply.
    /// </summary>
    public List<string> CssClasses { get; init; } = new();

    /// <summary>
    /// Gets inline style text.
    /// </summary>
    public string? InlineStyle { get; init; }

    /// <summary>
    /// Gets theme variables to use.
    /// </summary>
    public Dictionary<string, string> ThemeVariables { get; init; } = new();
}

/// <summary>
/// Represents layout information for UI components.
/// </summary>
public record UILayout
{
    /// <summary>
    /// Gets the layout type.
    /// </summary>
    public UILayoutType LayoutType { get; init; }

    /// <summary>
    /// Gets layout-specific properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();

    /// <summary>
    /// Gets responsive breakpoint configurations.
    /// </summary>
    public Dictionary<string, UILayout> ResponsiveLayouts { get; init; } = new();
}

/// <summary>
/// Represents virtual DOM-like node for performance optimization.
/// </summary>
public record UIVirtualNode
{
    /// <summary>
    /// Gets the node type.
    /// </summary>
    public string NodeType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the node properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();

    /// <summary>
    /// Gets the child nodes.
    /// </summary>
    public List<UIVirtualNode> Children { get; init; } = new();

    /// <summary>
    /// Gets the text content (for text nodes).
    /// </summary>
    public string? TextContent { get; init; }

    /// <summary>
    /// Gets the unique key for efficient diffing.
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// Gets metadata for framework-specific rendering.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Represents a UI component's render result.
/// </summary>
public record UIRenderResult
{
    /// <summary>
    /// Gets the rendered virtual node tree.
    /// </summary>
    public UIVirtualNode VirtualNode { get; init; } = new();

    /// <summary>
    /// Gets the render metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Gets whether the render was successful.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Gets any errors that occurred during rendering.
    /// </summary>
    public List<UIErrorEvent> Errors { get; init; } = new();

    /// <summary>
    /// Gets performance metrics for the render.
    /// </summary>
    public UIRenderMetrics Metrics { get; init; } = new();
}

/// <summary>
/// Performance metrics for UI rendering.
/// </summary>
public record UIRenderMetrics
{
    /// <summary>
    /// Gets the render duration in milliseconds.
    /// </summary>
    public double RenderDuration { get; init; }

    /// <summary>
    /// Gets the number of virtual nodes created.
    /// </summary>
    public int VirtualNodeCount { get; init; }

    /// <summary>
    /// Gets the number of DOM/framework nodes updated.
    /// </summary>
    public int UpdatedNodeCount { get; init; }

    /// <summary>
    /// Gets the memory usage in bytes.
    /// </summary>
    public long MemoryUsage { get; init; }
}

/// <summary>
/// Types of validation rules.
/// </summary>
public enum UIValidationRuleType
{
    /// <summary>
    /// Required field validation.
    /// </summary>
    Required,

    /// <summary>
    /// Minimum length validation.
    /// </summary>
    MinLength,

    /// <summary>
    /// Maximum length validation.
    /// </summary>
    MaxLength,

    /// <summary>
    /// Minimum value validation.
    /// </summary>
    MinValue,

    /// <summary>
    /// Maximum value validation.
    /// </summary>
    MaxValue,

    /// <summary>
    /// Regular expression validation.
    /// </summary>
    Regex,

    /// <summary>
    /// Email format validation.
    /// </summary>
    Email,

    /// <summary>
    /// URL format validation.
    /// </summary>
    Url,

    /// <summary>
    /// Custom validation function.
    /// </summary>
    Custom
}

/// <summary>
/// Types of UI layouts.
/// </summary>
public enum UILayoutType
{
    /// <summary>
    /// No specific layout (absolute positioning).
    /// </summary>
    None,

    /// <summary>
    /// Flexbox layout.
    /// </summary>
    Flex,

    /// <summary>
    /// CSS Grid layout.
    /// </summary>
    Grid,

    /// <summary>
    /// Stack layout (vertical or horizontal).
    /// </summary>
    Stack,

    /// <summary>
    /// Table layout.
    /// </summary>
    Table,

    /// <summary>
    /// Flow layout (wrapping).
    /// </summary>
    Flow
}