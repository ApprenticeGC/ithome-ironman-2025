using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for UI Profile interface contracts and behavior validation.
/// </summary>
public class UIProfileInterfaceTests
{
    [Fact]
    public void IUIProfile_ContractValidation_ShouldHaveRequiredProperties()
    {
        // Arrange
        var interfaceType = typeof(IUIProfile);
        
        // Act & Assert - Verify all required properties exist
        Assert.NotNull(interfaceType.GetProperty(nameof(IUIProfile.Id)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IUIProfile.Name)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IUIProfile.Description)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IUIProfile.Type)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IUIProfile.Configuration)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IUIProfile.IsActive)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IUIProfile.CreatedAt)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IUIProfile.LastModified)));
    }

    [Fact]
    public void IUIProfile_ContractValidation_ShouldHaveRequiredMethods()
    {
        // Arrange
        var interfaceType = typeof(IUIProfile);
        
        // Act & Assert - Verify required methods exist
        var getConfigMethod = interfaceType.GetMethod(nameof(IUIProfile.GetConfigurationValue));
        Assert.NotNull(getConfigMethod);
        Assert.True(getConfigMethod!.IsGenericMethod);
    }

    [Fact]
    public void IUIProfileManager_ContractValidation_ShouldHaveRequiredProperties()
    {
        // Arrange
        var interfaceType = typeof(IUIProfileManager);
        
        // Act & Assert - Verify all required properties exist
        Assert.NotNull(interfaceType.GetProperty(nameof(IUIProfileManager.Profiles)));
        Assert.NotNull(interfaceType.GetProperty(nameof(IUIProfileManager.ActiveProfile)));
    }

    [Fact]
    public void IUIProfileManager_ContractValidation_ShouldHaveRequiredMethods()
    {
        // Arrange
        var interfaceType = typeof(IUIProfileManager);
        var expectedMethods = new[]
        {
            nameof(IUIProfileManager.CreateProfileAsync),
            nameof(IUIProfileManager.GetProfile),
            nameof(IUIProfileManager.GetProfilesByType),
            nameof(IUIProfileManager.ActivateProfileAsync),
            nameof(IUIProfileManager.UpdateProfileAsync),
            nameof(IUIProfileManager.DeleteProfileAsync)
        };
        
        // Act & Assert - Verify all required methods exist
        foreach (var methodName in expectedMethods)
        {
            Assert.NotNull(interfaceType.GetMethod(methodName));
        }
    }

    [Fact]
    public void IUIProfileManager_ContractValidation_ShouldHaveRequiredEvents()
    {
        // Arrange
        var interfaceType = typeof(IUIProfileManager);
        var expectedEvents = new[]
        {
            nameof(IUIProfileManager.ProfileActivated),
            nameof(IUIProfileManager.ProfileCreated),
            nameof(IUIProfileManager.ProfileUpdated),
            nameof(IUIProfileManager.ProfileDeleted)
        };
        
        // Act & Assert - Verify all required events exist
        foreach (var eventName in expectedEvents)
        {
            Assert.NotNull(interfaceType.GetEvent(eventName));
        }
    }

    [Fact]
    public void IUIProfileConfigurationService_ContractValidation_ShouldInheritFromIService()
    {
        // Arrange
        var serviceInterface = typeof(IUIProfileConfigurationService);
        var baseInterface = typeof(GameConsole.Core.Abstractions.IService);
        
        // Act & Assert
        Assert.True(baseInterface.IsAssignableFrom(serviceInterface));
    }

    [Fact]
    public void IUIProfileConfigurationService_ContractValidation_ShouldHaveRequiredMethods()
    {
        // Arrange
        var interfaceType = typeof(IUIProfileConfigurationService);
        var expectedMethods = new[]
        {
            nameof(IUIProfileConfigurationService.LoadProfilesAsync),
            nameof(IUIProfileConfigurationService.SaveProfilesAsync),
            nameof(IUIProfileConfigurationService.ImportProfilesAsync),
            nameof(IUIProfileConfigurationService.ExportProfilesAsync),
            nameof(IUIProfileConfigurationService.ResetToDefaultProfilesAsync),
            nameof(IUIProfileConfigurationService.ValidateProfileAsync),
            nameof(IUIProfileConfigurationService.GetConfigurationSchema)
        };
        
        // Act & Assert - Verify all required methods exist
        foreach (var methodName in expectedMethods)
        {
            Assert.NotNull(interfaceType.GetMethod(methodName));
        }
    }
}