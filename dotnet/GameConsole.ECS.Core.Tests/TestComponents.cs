using GameConsole.ECS.Core;

namespace GameConsole.ECS.Core.Tests;

/// <summary>
/// Test component for position data.
/// </summary>
public class PositionComponent : IComponent
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public PositionComponent() { }

    public PositionComponent(float x, float y, float z = 0)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override bool Equals(object? obj)
    {
        return obj is PositionComponent other &&
               Math.Abs(X - other.X) < 0.001f &&
               Math.Abs(Y - other.Y) < 0.001f &&
               Math.Abs(Z - other.Z) < 0.001f;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public override string ToString()
    {
        return $"Position({X:F2}, {Y:F2}, {Z:F2})";
    }
}

/// <summary>
/// Test component for velocity data.
/// </summary>
public class VelocityComponent : IComponent
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public VelocityComponent() { }

    public VelocityComponent(float x, float y, float z = 0)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override bool Equals(object? obj)
    {
        return obj is VelocityComponent other &&
               Math.Abs(X - other.X) < 0.001f &&
               Math.Abs(Y - other.Y) < 0.001f &&
               Math.Abs(Z - other.Z) < 0.001f;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public override string ToString()
    {
        return $"Velocity({X:F2}, {Y:F2}, {Z:F2})";
    }
}

/// <summary>
/// Test component for health data.
/// </summary>
public class HealthComponent : IComponent
{
    public int Current { get; set; }
    public int Maximum { get; set; }

    public HealthComponent() { }

    public HealthComponent(int current, int maximum)
    {
        Current = current;
        Maximum = maximum;
    }

    public bool IsAlive => Current > 0;
    public bool IsFull => Current >= Maximum;
    public float Percentage => Maximum > 0 ? (float)Current / Maximum : 0f;

    public override bool Equals(object? obj)
    {
        return obj is HealthComponent other &&
               Current == other.Current &&
               Maximum == other.Maximum;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Current, Maximum);
    }

    public override string ToString()
    {
        return $"Health({Current}/{Maximum})";
    }
}

/// <summary>
/// Test component for name/identity data.
/// </summary>
public class NameComponent : IComponent
{
    public string Name { get; set; } = string.Empty;

    public NameComponent() { }

    public NameComponent(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public override bool Equals(object? obj)
    {
        return obj is NameComponent other && Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public override string ToString()
    {
        return $"Name({Name})";
    }
}