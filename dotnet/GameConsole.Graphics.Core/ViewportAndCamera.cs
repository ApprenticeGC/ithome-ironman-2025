using System.Numerics;

namespace GameConsole.Graphics.Core;

/// <summary>
/// Interface for viewport management and screen coordinate transformations.
/// </summary>
public interface IViewport
{
    /// <summary>
    /// Gets the x coordinate of the viewport in screen space.
    /// </summary>
    int X { get; }

    /// <summary>
    /// Gets the y coordinate of the viewport in screen space.
    /// </summary>
    int Y { get; }

    /// <summary>
    /// Gets the width of the viewport in pixels.
    /// </summary>
    uint Width { get; }

    /// <summary>
    /// Gets the height of the viewport in pixels.
    /// </summary>
    uint Height { get; }

    /// <summary>
    /// Gets the minimum depth value for the viewport.
    /// </summary>
    float MinDepth { get; }

    /// <summary>
    /// Gets the maximum depth value for the viewport.
    /// </summary>
    float MaxDepth { get; }

    /// <summary>
    /// Gets the aspect ratio (width/height) of the viewport.
    /// </summary>
    float AspectRatio { get; }

    /// <summary>
    /// Gets the viewport bounds as a rectangle.
    /// </summary>
    Rectangle Bounds { get; }

    /// <summary>
    /// Transforms a point from world space to screen space.
    /// </summary>
    /// <param name="worldPoint">The point in world space.</param>
    /// <param name="viewMatrix">The view transformation matrix.</param>
    /// <param name="projectionMatrix">The projection transformation matrix.</param>
    /// <returns>The point in screen space.</returns>
    Vector3 WorldToScreen(Vector3 worldPoint, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix);

    /// <summary>
    /// Transforms a point from screen space to world space.
    /// </summary>
    /// <param name="screenPoint">The point in screen space.</param>
    /// <param name="viewMatrix">The view transformation matrix.</param>
    /// <param name="projectionMatrix">The projection transformation matrix.</param>
    /// <returns>The point in world space.</returns>
    Vector3 ScreenToWorld(Vector3 screenPoint, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix);

    /// <summary>
    /// Creates a ray from the camera through the specified screen point.
    /// </summary>
    /// <param name="screenPoint">The point in screen space.</param>
    /// <param name="viewMatrix">The view transformation matrix.</param>
    /// <param name="projectionMatrix">The projection transformation matrix.</param>
    /// <returns>A ray from the camera through the screen point.</returns>
    Ray ScreenPointToRay(Vector2 screenPoint, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix);

    /// <summary>
    /// Checks if a point is inside the viewport bounds.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <returns>True if the point is inside the viewport, false otherwise.</returns>
    bool Contains(Vector2 point);

    /// <summary>
    /// Updates the viewport configuration.
    /// </summary>
    /// <param name="x">The new x coordinate.</param>
    /// <param name="y">The new y coordinate.</param>
    /// <param name="width">The new width.</param>
    /// <param name="height">The new height.</param>
    /// <param name="minDepth">The new minimum depth value.</param>
    /// <param name="maxDepth">The new maximum depth value.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UpdateAsync(int x, int y, uint width, uint height, float minDepth = 0.0f, float maxDepth = 1.0f, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for camera management and view/projection matrix calculations.
/// </summary>
public interface ICamera
{
    /// <summary>
    /// Gets the position of the camera in world space.
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// Gets the forward direction vector of the camera.
    /// </summary>
    Vector3 Forward { get; }

    /// <summary>
    /// Gets the up direction vector of the camera.
    /// </summary>
    Vector3 Up { get; }

    /// <summary>
    /// Gets the right direction vector of the camera.
    /// </summary>
    Vector3 Right { get; }

    /// <summary>
    /// Gets the rotation of the camera as a quaternion.
    /// </summary>
    Quaternion Rotation { get; }

    /// <summary>
    /// Gets the field of view in radians (for perspective cameras).
    /// </summary>
    float FieldOfView { get; }

    /// <summary>
    /// Gets the aspect ratio of the camera.
    /// </summary>
    float AspectRatio { get; }

    /// <summary>
    /// Gets the near clipping plane distance.
    /// </summary>
    float NearPlane { get; }

    /// <summary>
    /// Gets the far clipping plane distance.
    /// </summary>
    float FarPlane { get; }

    /// <summary>
    /// Gets the projection type of the camera.
    /// </summary>
    ProjectionType ProjectionType { get; }

    /// <summary>
    /// Gets the orthographic size (for orthographic cameras).
    /// </summary>
    float OrthographicSize { get; }

    /// <summary>
    /// Gets the view matrix for the camera.
    /// </summary>
    Matrix4x4 ViewMatrix { get; }

    /// <summary>
    /// Gets the projection matrix for the camera.
    /// </summary>
    Matrix4x4 ProjectionMatrix { get; }

    /// <summary>
    /// Gets the combined view-projection matrix.
    /// </summary>
    Matrix4x4 ViewProjectionMatrix { get; }

    /// <summary>
    /// Gets the frustum for the camera used for culling.
    /// </summary>
    Frustum Frustum { get; }

    /// <summary>
    /// Sets the position of the camera.
    /// </summary>
    /// <param name="position">The new position.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetPositionAsync(Vector3 position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the rotation of the camera.
    /// </summary>
    /// <param name="rotation">The new rotation as a quaternion.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetRotationAsync(Quaternion rotation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the camera to look at a specific target.
    /// </summary>
    /// <param name="target">The target position to look at.</param>
    /// <param name="up">The up direction vector.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task LookAtAsync(Vector3 target, Vector3? up = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the perspective projection parameters.
    /// </summary>
    /// <param name="fieldOfView">The field of view in radians.</param>
    /// <param name="aspectRatio">The aspect ratio.</param>
    /// <param name="nearPlane">The near clipping plane distance.</param>
    /// <param name="farPlane">The far clipping plane distance.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetPerspectiveProjectionAsync(float fieldOfView, float aspectRatio, float nearPlane, float farPlane, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the orthographic projection parameters.
    /// </summary>
    /// <param name="size">The orthographic size.</param>
    /// <param name="aspectRatio">The aspect ratio.</param>
    /// <param name="nearPlane">The near clipping plane distance.</param>
    /// <param name="farPlane">The far clipping plane distance.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetOrthographicProjectionAsync(float size, float aspectRatio, float nearPlane, float farPlane, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves the camera by the specified offset.
    /// </summary>
    /// <param name="offset">The movement offset.</param>
    /// <param name="relativeTo">The coordinate space for the movement.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task MoveAsync(Vector3 offset, Space relativeTo = Space.World, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates the camera by the specified angles.
    /// </summary>
    /// <param name="eulerAngles">The rotation angles in radians (pitch, yaw, roll).</param>
    /// <param name="relativeTo">The coordinate space for the rotation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RotateAsync(Vector3 eulerAngles, Space relativeTo = Space.Local, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a 3D ray with an origin and direction.
/// </summary>
public record struct Ray
{
    /// <summary>
    /// Gets or sets the origin point of the ray.
    /// </summary>
    public Vector3 Origin { get; set; }

    /// <summary>
    /// Gets or sets the direction vector of the ray.
    /// </summary>
    public Vector3 Direction { get; set; }

    /// <summary>
    /// Initializes a new instance of the Ray struct.
    /// </summary>
    /// <param name="origin">The origin point of the ray.</param>
    /// <param name="direction">The direction vector of the ray.</param>
    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = Vector3.Normalize(direction);
    }

    /// <summary>
    /// Gets a point along the ray at the specified distance.
    /// </summary>
    /// <param name="distance">The distance along the ray.</param>
    /// <returns>The point at the specified distance.</returns>
    public Vector3 GetPoint(float distance) => Origin + Direction * distance;
}

/// <summary>
/// Represents a viewing frustum for culling.
/// </summary>
public record struct Frustum
{
    /// <summary>
    /// Gets or sets the planes that define the frustum.
    /// </summary>
    public Plane[] Planes { get; set; }

    /// <summary>
    /// Gets or sets the corners of the frustum.
    /// </summary>
    public Vector3[] Corners { get; set; }

    /// <summary>
    /// Checks if a point is inside the frustum.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <returns>True if the point is inside the frustum, false otherwise.</returns>
    public bool Contains(Vector3 point)
    {
        foreach (var plane in Planes)
        {
            if (Vector3.Dot(point, plane.Normal) + plane.D < 0)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if a sphere intersects with the frustum.
    /// </summary>
    /// <param name="center">The center of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <returns>True if the sphere intersects the frustum, false otherwise.</returns>
    public bool Intersects(Vector3 center, float radius)
    {
        foreach (var plane in Planes)
        {
            if (Vector3.Dot(center, plane.Normal) + plane.D < -radius)
                return false;
        }
        return true;
    }
}

/// <summary>
/// Defines the projection type for cameras.
/// </summary>
public enum ProjectionType
{
    /// <summary>
    /// Perspective projection.
    /// </summary>
    Perspective,

    /// <summary>
    /// Orthographic projection.
    /// </summary>
    Orthographic
}

/// <summary>
/// Defines coordinate spaces for transformations.
/// </summary>
public enum Space
{
    /// <summary>
    /// World coordinate space.
    /// </summary>
    World,

    /// <summary>
    /// Local coordinate space.
    /// </summary>
    Local
}