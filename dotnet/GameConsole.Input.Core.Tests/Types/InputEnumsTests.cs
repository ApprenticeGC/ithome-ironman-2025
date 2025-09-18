using GameConsole.Input.Core.Types;
using Xunit;

namespace GameConsole.Input.Core.Tests.Types;

public class InputEnumsTests
{
    [Fact]
    public void KeyCode_HasExpectedValues()
    {
        // Test some key values exist
        Assert.Equal(0, (int)KeyCode.Unknown);
        Assert.True(Enum.IsDefined(typeof(KeyCode), KeyCode.A));
        Assert.True(Enum.IsDefined(typeof(KeyCode), KeyCode.F1));
        Assert.True(Enum.IsDefined(typeof(KeyCode), KeyCode.LeftArrow));
        Assert.True(Enum.IsDefined(typeof(KeyCode), KeyCode.Space));
        Assert.True(Enum.IsDefined(typeof(KeyCode), KeyCode.Enter));
    }

    [Fact]
    public void MouseButton_HasExpectedValues()
    {
        // Test mouse button values
        Assert.Equal(0, (int)MouseButton.Left);
        Assert.Equal(1, (int)MouseButton.Right);
        Assert.Equal(2, (int)MouseButton.Middle);
        Assert.Equal(3, (int)MouseButton.Button4);
        Assert.Equal(4, (int)MouseButton.Button5);
    }

    [Fact]
    public void GamepadButton_HasExpectedValues()
    {
        // Test gamepad button values exist
        Assert.True(Enum.IsDefined(typeof(GamepadButton), GamepadButton.FaceButtonSouth));
        Assert.True(Enum.IsDefined(typeof(GamepadButton), GamepadButton.FaceButtonEast));
        Assert.True(Enum.IsDefined(typeof(GamepadButton), GamepadButton.FaceButtonWest));
        Assert.True(Enum.IsDefined(typeof(GamepadButton), GamepadButton.FaceButtonNorth));
        Assert.True(Enum.IsDefined(typeof(GamepadButton), GamepadButton.LeftShoulder));
        Assert.True(Enum.IsDefined(typeof(GamepadButton), GamepadButton.RightShoulder));
        Assert.True(Enum.IsDefined(typeof(GamepadButton), GamepadButton.Start));
        Assert.True(Enum.IsDefined(typeof(GamepadButton), GamepadButton.Select));
    }

    [Fact]
    public void GamepadAxis_HasExpectedValues()
    {
        // Test gamepad axis values exist
        Assert.True(Enum.IsDefined(typeof(GamepadAxis), GamepadAxis.LeftStickX));
        Assert.True(Enum.IsDefined(typeof(GamepadAxis), GamepadAxis.LeftStickY));
        Assert.True(Enum.IsDefined(typeof(GamepadAxis), GamepadAxis.RightStickX));
        Assert.True(Enum.IsDefined(typeof(GamepadAxis), GamepadAxis.RightStickY));
        Assert.True(Enum.IsDefined(typeof(GamepadAxis), GamepadAxis.LeftTrigger));
        Assert.True(Enum.IsDefined(typeof(GamepadAxis), GamepadAxis.RightTrigger));
    }

    [Theory]
    [InlineData(KeyCode.A)]
    [InlineData(KeyCode.Z)]
    [InlineData(KeyCode.D0)]
    [InlineData(KeyCode.D9)]
    [InlineData(KeyCode.F1)]
    [InlineData(KeyCode.F12)]
    public void KeyCode_AllDefinedValuesAreValid(KeyCode keyCode)
    {
        // Act & Assert - no exception should be thrown
        Assert.True(Enum.IsDefined(typeof(KeyCode), keyCode));
    }

    [Theory]
    [InlineData(MouseButton.Left)]
    [InlineData(MouseButton.Right)]
    [InlineData(MouseButton.Middle)]
    [InlineData(MouseButton.Button4)]
    [InlineData(MouseButton.Button5)]
    public void MouseButton_AllDefinedValuesAreValid(MouseButton button)
    {
        // Act & Assert - no exception should be thrown
        Assert.True(Enum.IsDefined(typeof(MouseButton), button));
    }

    [Theory]
    [InlineData(GamepadButton.FaceButtonSouth)]
    [InlineData(GamepadButton.FaceButtonEast)]
    [InlineData(GamepadButton.FaceButtonWest)]
    [InlineData(GamepadButton.FaceButtonNorth)]
    [InlineData(GamepadButton.LeftShoulder)]
    [InlineData(GamepadButton.RightShoulder)]
    [InlineData(GamepadButton.DPadUp)]
    [InlineData(GamepadButton.DPadDown)]
    [InlineData(GamepadButton.DPadLeft)]
    [InlineData(GamepadButton.DPadRight)]
    public void GamepadButton_AllDefinedValuesAreValid(GamepadButton button)
    {
        // Act & Assert - no exception should be thrown
        Assert.True(Enum.IsDefined(typeof(GamepadButton), button));
    }

    [Theory]
    [InlineData(GamepadAxis.LeftStickX)]
    [InlineData(GamepadAxis.LeftStickY)]
    [InlineData(GamepadAxis.RightStickX)]
    [InlineData(GamepadAxis.RightStickY)]
    [InlineData(GamepadAxis.LeftTrigger)]
    [InlineData(GamepadAxis.RightTrigger)]
    public void GamepadAxis_AllDefinedValuesAreValid(GamepadAxis axis)
    {
        // Act & Assert - no exception should be thrown
        Assert.True(Enum.IsDefined(typeof(GamepadAxis), axis));
    }
}