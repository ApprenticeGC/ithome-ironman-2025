using GameConsole.UI.Core;
using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests to verify the interface contracts and behavior for GameConsole.UI.Core.
/// </summary>
public class UIInterfaceContractTests
{
    [Fact]
    public void IService_Should_Inherit_From_Base_Service()
    {
        // Arrange
        var uiServiceType = typeof(GameConsole.UI.Core.IService);
        var baseServiceType = typeof(GameConsole.Core.Abstractions.IService);

        // Act & Assert
        Assert.True(baseServiceType.IsAssignableFrom(uiServiceType));
    }

    [Fact]
    public void IService_Should_Have_Required_UI_Methods()
    {
        // Arrange
        var serviceType = typeof(GameConsole.UI.Core.IService);

        // Act & Assert
        Assert.NotNull(serviceType.GetMethod("DisplayMessageAsync"));
        Assert.NotNull(serviceType.GetMethod("DisplayMenuAsync"));
        Assert.NotNull(serviceType.GetMethod("ClearDisplayAsync"));
        Assert.NotNull(serviceType.GetMethod("PromptInputAsync"));
    }

    [Fact]
    public void IMessageFormattingCapability_Should_Inherit_From_CapabilityProvider()
    {
        // Arrange
        var capabilityType = typeof(IMessageFormattingCapability);
        var baseCapabilityType = typeof(ICapabilityProvider);

        // Act & Assert
        Assert.True(baseCapabilityType.IsAssignableFrom(capabilityType));
    }

    [Fact]
    public void IMenuNavigationCapability_Should_Inherit_From_CapabilityProvider()
    {
        // Arrange
        var capabilityType = typeof(IMenuNavigationCapability);
        var baseCapabilityType = typeof(ICapabilityProvider);

        // Act & Assert
        Assert.True(baseCapabilityType.IsAssignableFrom(capabilityType));
    }

    [Fact]
    public void DisplayMessageAsync_Should_Accept_UIMessage_And_CancellationToken()
    {
        // Arrange
        var serviceType = typeof(GameConsole.UI.Core.IService);
        var method = serviceType.GetMethod("DisplayMessageAsync");

        // Act & Assert
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(UIMessage), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void DisplayMenuAsync_Should_Accept_Menu_And_Return_String()
    {
        // Arrange
        var serviceType = typeof(GameConsole.UI.Core.IService);
        var method = serviceType.GetMethod("DisplayMenuAsync");

        // Act & Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<string>), method.ReturnType);
        
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(Menu), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void PromptInputAsync_Should_Accept_String_And_Return_String()
    {
        // Arrange
        var serviceType = typeof(GameConsole.UI.Core.IService);
        var method = serviceType.GetMethod("PromptInputAsync");

        // Act & Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<string>), method.ReturnType);
        
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void ClearDisplayAsync_Should_Have_Correct_Signature()
    {
        // Arrange
        var serviceType = typeof(GameConsole.UI.Core.IService);
        var method = serviceType.GetMethod("ClearDisplayAsync");

        // Act & Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
    }
}