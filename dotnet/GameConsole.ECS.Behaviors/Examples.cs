namespace GameConsole.ECS.Behaviors.Examples;

/// <summary>
/// Example component for entity positioning.
/// </summary>
public class PositionComponent
{
    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Gets or sets the Z coordinate.
    /// </summary>
    public float Z { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"Position({X}, {Y}, {Z})";
}

/// <summary>
/// Example component for entity health management.
/// </summary>
public class HealthComponent
{
    /// <summary>
    /// Gets or sets the current health value.
    /// </summary>
    public int CurrentHealth { get; set; }

    /// <summary>
    /// Gets or sets the maximum health value.
    /// </summary>
    public int MaxHealth { get; set; }

    /// <summary>
    /// Gets a value indicating whether the entity is alive.
    /// </summary>
    public bool IsAlive => CurrentHealth > 0;

    /// <summary>
    /// Gets the health percentage (0.0 to 1.0).
    /// </summary>
    public float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;

    /// <inheritdoc />
    public override string ToString() => $"Health({CurrentHealth}/{MaxHealth})";
}

/// <summary>
/// Example component for entity movement.
/// </summary>
public class MovementComponent
{
    /// <summary>
    /// Gets or sets the movement speed.
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// Gets or sets the velocity in X direction.
    /// </summary>
    public float VelocityX { get; set; }

    /// <summary>
    /// Gets or sets the velocity in Y direction.
    /// </summary>
    public float VelocityY { get; set; }

    /// <summary>
    /// Gets or sets the velocity in Z direction.
    /// </summary>
    public float VelocityZ { get; set; }

    /// <summary>
    /// Gets a value indicating whether the entity is moving.
    /// </summary>
    public bool IsMoving => Math.Abs(VelocityX) > 0.001f || Math.Abs(VelocityY) > 0.001f || Math.Abs(VelocityZ) > 0.001f;

    /// <inheritdoc />
    public override string ToString() => $"Movement(Speed: {Speed}, Velocity: ({VelocityX}, {VelocityY}, {VelocityZ}))";
}

/// <summary>
/// Example component for weapon handling.
/// </summary>
public class WeaponComponent
{
    /// <summary>
    /// Gets or sets the weapon damage.
    /// </summary>
    public int Damage { get; set; }

    /// <summary>
    /// Gets or sets the weapon range.
    /// </summary>
    public float Range { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the weapon attack rate (attacks per second).
    /// </summary>
    public float AttackRate { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the weapon name.
    /// </summary>
    public string Name { get; set; } = "Basic Weapon";

    /// <inheritdoc />
    public override string ToString() => $"Weapon({Name}, Damage: {Damage}, Range: {Range})";
}

/// <summary>
/// Example component for AI behavior.
/// </summary>
public class AiComponent
{
    /// <summary>
    /// Gets or sets the AI behavior type.
    /// </summary>
    public AiBehaviorType BehaviorType { get; set; } = AiBehaviorType.Passive;

    /// <summary>
    /// Gets or sets the AI aggression level (0.0 to 1.0).
    /// </summary>
    public float AggressionLevel { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the AI sight range.
    /// </summary>
    public float SightRange { get; set; } = 10.0f;

    /// <summary>
    /// Gets or sets the current AI state.
    /// </summary>
    public string CurrentState { get; set; } = "Idle";

    /// <inheritdoc />
    public override string ToString() => $"AI({BehaviorType}, State: {CurrentState}, Aggression: {AggressionLevel})";
}

/// <summary>
/// AI behavior types.
/// </summary>
public enum AiBehaviorType
{
    /// <summary>
    /// Passive AI that doesn't attack unless provoked.
    /// </summary>
    Passive,

    /// <summary>
    /// Aggressive AI that attacks on sight.
    /// </summary>
    Aggressive,

    /// <summary>
    /// Defensive AI that defends a specific area.
    /// </summary>
    Defensive,

    /// <summary>
    /// Patrol AI that follows a predefined route.
    /// </summary>
    Patrol
}

/// <summary>
/// Example component for inventory management.
/// </summary>
public class InventoryComponent
{
    private readonly Dictionary<string, int> _items = new();

    /// <summary>
    /// Gets or sets the maximum inventory capacity.
    /// </summary>
    public int MaxCapacity { get; set; } = 20;

    /// <summary>
    /// Gets the current number of items in the inventory.
    /// </summary>
    public int CurrentCount => _items.Values.Sum();

    /// <summary>
    /// Gets a read-only view of the inventory items.
    /// </summary>
    public IReadOnlyDictionary<string, int> Items => _items;

    /// <summary>
    /// Adds an item to the inventory.
    /// </summary>
    /// <param name="itemName">The name of the item to add.</param>
    /// <param name="quantity">The quantity to add.</param>
    /// <returns>True if the item was added successfully.</returns>
    public bool AddItem(string itemName, int quantity = 1)
    {
        if (CurrentCount + quantity > MaxCapacity)
            return false;

        _items[itemName] = _items.GetValueOrDefault(itemName, 0) + quantity;
        return true;
    }

    /// <summary>
    /// Removes an item from the inventory.
    /// </summary>
    /// <param name="itemName">The name of the item to remove.</param>
    /// <param name="quantity">The quantity to remove.</param>
    /// <returns>True if the item was removed successfully.</returns>
    public bool RemoveItem(string itemName, int quantity = 1)
    {
        if (!_items.ContainsKey(itemName) || _items[itemName] < quantity)
            return false;

        _items[itemName] -= quantity;
        if (_items[itemName] == 0)
            _items.Remove(itemName);

        return true;
    }

    /// <inheritdoc />
    public override string ToString() => $"Inventory({CurrentCount}/{MaxCapacity} items: {string.Join(", ", _items.Select(kv => $"{kv.Key}x{kv.Value}"))})";
}

/// <summary>
/// Example component for rendering visual representation.
/// </summary>
public class RenderComponent
{
    /// <summary>
    /// Gets or sets the sprite or model identifier.
    /// </summary>
    public string SpriteId { get; set; } = "default";

    /// <summary>
    /// Gets or sets the render layer.
    /// </summary>
    public int Layer { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether the component is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the opacity (0.0 to 1.0).
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the color tint.
    /// </summary>
    public string Color { get; set; } = "#FFFFFF";

    /// <inheritdoc />
    public override string ToString() => $"Render(Sprite: {SpriteId}, Layer: {Layer}, Visible: {IsVisible})";
}