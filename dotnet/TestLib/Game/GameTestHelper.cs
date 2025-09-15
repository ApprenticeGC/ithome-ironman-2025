namespace TestLib.Game;

/// <summary>
/// Provides utility methods for testing game components.
/// </summary>
public static class GameTestHelper
{
    /// <summary>
    /// Creates a test game component with the specified name.
    /// </summary>
    /// <param name="name">The name of the component.</param>
    /// <returns>A test implementation of IGameComponent.</returns>
    public static IGameComponent CreateTestComponent(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new TestGameComponent(name);
    }

    /// <summary>
    /// Validates that a game component is properly initialized.
    /// </summary>
    /// <param name="component">The component to validate.</param>
    /// <returns>True if the component is valid; otherwise, false.</returns>
    public static bool ValidateComponent(IGameComponent component)
    {
        if (component == null)
            return false;

        if (string.IsNullOrWhiteSpace(component.Name))
            return false;

        return true;
    }

    private sealed class TestGameComponent : IGameComponent
    {
        public TestGameComponent(string name)
        {
            Name = name;
            IsActive = true;
        }

        public string Name { get; }

        public bool IsActive { get; set; }

        public void Update()
        {
            // Test implementation - no-op
        }
    }
}