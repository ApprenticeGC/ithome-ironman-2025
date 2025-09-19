namespace GameConsole.Input.Core;

/// <summary>
/// Represents a 2D vector with X and Y components.
/// Used for mouse positions, analog stick values, and other 2D input data.
/// </summary>
public struct Vector2
{
    /// <summary>
    /// The X component of the vector.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The Y component of the vector.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Initializes a new instance of the Vector2 struct.
    /// </summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// A Vector2 with both components set to zero.
    /// </summary>
    public static readonly Vector2 Zero = new(0, 0);

    /// <summary>
    /// A Vector2 with both components set to one.
    /// </summary>
    public static readonly Vector2 One = new(1, 1);

    /// <summary>
    /// Returns a string representation of the vector.
    /// </summary>
    public override string ToString() => $"({X}, {Y})";
}

/// <summary>
/// Represents a 3D vector with X, Y, and Z components.
/// Used for spatial audio positioning and 3D input data.
/// </summary>
public struct Vector3
{
    /// <summary>
    /// The X component of the vector.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The Y component of the vector.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// The Z component of the vector.
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// Initializes a new instance of the Vector3 struct.
    /// </summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// A Vector3 with all components set to zero.
    /// </summary>
    public static readonly Vector3 Zero = new(0, 0, 0);

    /// <summary>
    /// A Vector3 with all components set to one.
    /// </summary>
    public static readonly Vector3 One = new(1, 1, 1);

    /// <summary>
    /// Returns a string representation of the vector.
    /// </summary>
    public override string ToString() => $"({X}, {Y}, {Z})";
}