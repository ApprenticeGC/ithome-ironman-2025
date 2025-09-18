namespace GameConsole.Input.Core.Types;

/// <summary>
/// Represents a two-dimensional vector with X and Y components.
/// Used for mouse positions, analog stick values, and other 2D input data.
/// </summary>
public readonly struct Vector2 : IEquatable<Vector2>
{
    /// <summary>
    /// Gets the X component of the vector.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the Y component of the vector.
    /// </summary>
    public float Y { get; }

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
    /// Gets a vector with both components set to zero.
    /// </summary>
    public static Vector2 Zero => new(0f, 0f);

    /// <summary>
    /// Gets a vector with both components set to one.
    /// </summary>
    public static Vector2 One => new(1f, 1f);

    /// <summary>
    /// Gets the magnitude (length) of the vector.
    /// </summary>
    public float Magnitude => MathF.Sqrt(X * X + Y * Y);

    /// <summary>
    /// Gets the squared magnitude of the vector (more efficient than Magnitude).
    /// </summary>
    public float SqrMagnitude => X * X + Y * Y;

    /// <summary>
    /// Returns a normalized version of this vector.
    /// </summary>
    public Vector2 Normalized
    {
        get
        {
            var magnitude = Magnitude;
            return magnitude > 0 ? new Vector2(X / magnitude, Y / magnitude) : Zero;
        }
    }

    public bool Equals(Vector2 other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override string ToString()
    {
        return $"({X:F2}, {Y:F2})";
    }

    public static bool operator ==(Vector2 left, Vector2 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector2 left, Vector2 right)
    {
        return !left.Equals(right);
    }

    public static Vector2 operator +(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X + right.X, left.Y + right.Y);
    }

    public static Vector2 operator -(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X - right.X, left.Y - right.Y);
    }

    public static Vector2 operator *(Vector2 vector, float scalar)
    {
        return new Vector2(vector.X * scalar, vector.Y * scalar);
    }

    public static Vector2 operator *(float scalar, Vector2 vector)
    {
        return vector * scalar;
    }
}