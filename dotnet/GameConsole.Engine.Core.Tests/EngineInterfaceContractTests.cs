using GameConsole.Core.Abstractions;
using GameConsole.Engine.Core;
using Xunit;

namespace GameConsole.Engine.Core.Tests;

/// <summary>
/// Tests to verify the interface contracts and behavior for GameConsole.Engine.Core.
/// </summary>
public class EngineInterfaceContractTests
{
    [Fact]
    public void ISceneManager_Should_Inherit_From_IService()
    {
        // Arrange & Act
        var sceneManagerType = typeof(ISceneManager);
        
        // Assert
        Assert.True(typeof(IService).IsAssignableFrom(sceneManagerType));
    }

    [Fact]
    public void IResourceManager_Should_Inherit_From_IService()
    {
        // Arrange & Act
        var resourceManagerType = typeof(IResourceManager);
        
        // Assert
        Assert.True(typeof(IService).IsAssignableFrom(resourceManagerType));
    }

    [Fact]
    public void IUpdateLoop_Should_Inherit_From_IService()
    {
        // Arrange & Act
        var updateLoopType = typeof(IUpdateLoop);
        
        // Assert
        Assert.True(typeof(IService).IsAssignableFrom(updateLoopType));
    }

    [Fact]
    public void IPhysicsService_Should_Inherit_From_IService()
    {
        // Arrange & Act
        var physicsServiceType = typeof(IPhysicsService);
        
        // Assert
        Assert.True(typeof(IService).IsAssignableFrom(physicsServiceType));
    }

    [Fact]
    public void IFrameRateManager_Should_Inherit_From_IService()
    {
        // Arrange & Act
        var frameRateManagerType = typeof(IFrameRateManager);
        
        // Assert
        Assert.True(typeof(IService).IsAssignableFrom(frameRateManagerType));
    }

    [Fact]
    public void ISceneManager_Should_Have_Required_Scene_Management_Methods()
    {
        // Arrange
        var sceneManagerType = typeof(ISceneManager);
        
        // Act & Assert - Check for core scene management methods
        var loadSceneMethod = sceneManagerType.GetMethod("LoadSceneAsync");
        Assert.NotNull(loadSceneMethod);
        Assert.Equal(typeof(Task), loadSceneMethod.ReturnType);
        
        var unloadSceneMethod = sceneManagerType.GetMethod("UnloadSceneAsync");
        Assert.NotNull(unloadSceneMethod);
        Assert.Equal(typeof(Task), unloadSceneMethod.ReturnType);
        
        var transitionMethod = sceneManagerType.GetMethod("TransitionToSceneAsync");
        Assert.NotNull(transitionMethod);
        Assert.Equal(typeof(Task), transitionMethod.ReturnType);
        
        var activeSceneProperty = sceneManagerType.GetProperty("ActiveSceneId");
        Assert.NotNull(activeSceneProperty);
        Assert.Equal(typeof(string), activeSceneProperty.PropertyType);
    }

    [Fact]
    public void IResourceManager_Should_Have_Required_Resource_Methods()
    {
        // Arrange
        var resourceManagerType = typeof(IResourceManager);
        
        // Act & Assert - Check for core resource management methods
        var loadResourceMethod = resourceManagerType.GetMethod("LoadResourceAsync");
        Assert.NotNull(loadResourceMethod);
        Assert.True(loadResourceMethod.IsGenericMethod);
        
        var loadWithDependenciesMethod = resourceManagerType.GetMethod("LoadWithDependenciesAsync");
        Assert.NotNull(loadWithDependenciesMethod);
        Assert.True(loadWithDependenciesMethod.IsGenericMethod);
        
        var unloadResourceMethod = resourceManagerType.GetMethod("UnloadResourceAsync");
        Assert.NotNull(unloadResourceMethod);
        Assert.Equal(typeof(Task), unloadResourceMethod.ReturnType);
        
        var isLoadedMethod = resourceManagerType.GetMethod("IsResourceLoadedAsync");
        Assert.NotNull(isLoadedMethod);
        Assert.Equal(typeof(Task<bool>), isLoadedMethod.ReturnType);
    }

    [Fact]
    public void IUpdateLoop_Should_Have_Required_Update_Methods()
    {
        // Arrange
        var updateLoopType = typeof(IUpdateLoop);
        
        // Act & Assert - Check for core update loop methods
        var registerUpdateMethod = updateLoopType.GetMethod("RegisterUpdateAsync");
        Assert.NotNull(registerUpdateMethod);
        Assert.Equal(typeof(Task<UpdateRegistration>), registerUpdateMethod.ReturnType);
        
        var registerFixedUpdateMethod = updateLoopType.GetMethod("RegisterFixedUpdateAsync");
        Assert.NotNull(registerFixedUpdateMethod);
        Assert.Equal(typeof(Task<UpdateRegistration>), registerFixedUpdateMethod.ReturnType);
        
        var setUpdateModeMethod = updateLoopType.GetMethod("SetUpdateModeAsync");
        Assert.NotNull(setUpdateModeMethod);
        Assert.Equal(typeof(Task), setUpdateModeMethod.ReturnType);
        
        var deltaTimeProperty = updateLoopType.GetProperty("DeltaTime");
        Assert.NotNull(deltaTimeProperty);
        Assert.Equal(typeof(float), deltaTimeProperty.PropertyType);
    }

    [Fact]
    public void IPhysicsService_Should_Have_Required_Physics_Methods()
    {
        // Arrange
        var physicsServiceType = typeof(IPhysicsService);
        
        // Act & Assert - Check for core physics methods
        var stepSimulationMethod = physicsServiceType.GetMethod("StepSimulationAsync");
        Assert.NotNull(stepSimulationMethod);
        Assert.Equal(typeof(Task), stepSimulationMethod.ReturnType);
        
        var raycastMethod = physicsServiceType.GetMethod("RaycastAsync");
        Assert.NotNull(raycastMethod);
        Assert.Equal(typeof(Task<RaycastHit?>), raycastMethod.ReturnType);
        
        var createBodyMethod = physicsServiceType.GetMethod("CreateBodyAsync");
        Assert.NotNull(createBodyMethod);
        Assert.Equal(typeof(Task), createBodyMethod.ReturnType);
        
        var gravityProperty = physicsServiceType.GetProperty("Gravity");
        Assert.NotNull(gravityProperty);
        Assert.Equal(typeof(Vector3), gravityProperty.PropertyType);
    }

    [Fact]
    public void IFrameRateManager_Should_Have_Required_FrameRate_Methods()
    {
        // Arrange
        var frameRateManagerType = typeof(IFrameRateManager);
        
        // Act & Assert - Check for core frame rate methods
        var setTargetFrameRateMethod = frameRateManagerType.GetMethod("SetTargetFrameRateAsync");
        Assert.NotNull(setTargetFrameRateMethod);
        Assert.Equal(typeof(Task), setTargetFrameRateMethod.ReturnType);
        
        var setVSyncModeMethod = frameRateManagerType.GetMethod("SetVSyncModeAsync");
        Assert.NotNull(setVSyncModeMethod);
        Assert.Equal(typeof(Task), setVSyncModeMethod.ReturnType);
        
        var getRefreshRateMethod = frameRateManagerType.GetMethod("GetDisplayRefreshRateAsync");
        Assert.NotNull(getRefreshRateMethod);
        Assert.Equal(typeof(Task<float>), getRefreshRateMethod.ReturnType);
        
        var targetFrameRateProperty = frameRateManagerType.GetProperty("TargetFrameRate");
        Assert.NotNull(targetFrameRateProperty);
        Assert.Equal(typeof(float), targetFrameRateProperty.PropertyType);
    }

    [Fact]
    public void SceneTransitionMode_Should_Have_Expected_Values()
    {
        // Arrange & Act
        var values = Enum.GetValues<SceneTransitionMode>();
        
        // Assert
        Assert.Contains(SceneTransitionMode.Additive, values);
        Assert.Contains(SceneTransitionMode.Replace, values);
        Assert.Contains(SceneTransitionMode.Background, values);
    }

    [Fact]
    public void UpdateMode_Should_Have_Expected_Values()
    {
        // Arrange & Act
        var values = Enum.GetValues<UpdateMode>();
        
        // Assert
        Assert.Contains(UpdateMode.Variable, values);
        Assert.Contains(UpdateMode.Fixed, values);
        Assert.Contains(UpdateMode.Hybrid, values);
    }

    [Fact]
    public void VSyncMode_Should_Have_Expected_Values()
    {
        // Arrange & Act
        var values = Enum.GetValues<VSyncMode>();
        
        // Assert
        Assert.Contains(VSyncMode.Disabled, values);
        Assert.Contains(VSyncMode.Enabled, values);
        Assert.Contains(VSyncMode.Adaptive, values);
        Assert.Contains(VSyncMode.Half, values);
    }

    [Fact]
    public void PhysicsStepMode_Should_Have_Expected_Values()
    {
        // Arrange & Act
        var values = Enum.GetValues<PhysicsStepMode>();
        
        // Assert
        Assert.Contains(PhysicsStepMode.Automatic, values);
        Assert.Contains(PhysicsStepMode.Manual, values);
        Assert.Contains(PhysicsStepMode.FixedTimestep, values);
    }

    [Fact]
    public void Vector3_Should_Have_Static_Properties()
    {
        // Arrange & Act & Assert
        Assert.Equal(new Vector3(0, 0, 0), Vector3.Zero);
        Assert.Equal(new Vector3(1, 1, 1), Vector3.One);
        Assert.Equal(new Vector3(0, 1, 0), Vector3.Up);
    }

    [Fact]
    public void ResourceDependency_Should_Create_Correctly()
    {
        // Arrange
        var resourceId = "test-resource";
        var resourceType = typeof(string);
        var isRequired = true;

        // Act
        var dependency = new ResourceDependency(resourceId, resourceType, isRequired);

        // Assert
        Assert.Equal(resourceId, dependency.ResourceId);
        Assert.Equal(resourceType, dependency.ResourceType);
        Assert.Equal(isRequired, dependency.IsRequired);
    }

    [Fact]
    public void UpdateRegistration_Should_Create_With_Callback()
    {
        // Arrange
        Action<UpdateEventArgs> callback = _ => { };
        var priority = UpdatePriority.High;

        // Act
        var registration = new UpdateRegistration(callback, priority);

        // Assert
        Assert.Equal(callback, registration.Callback);
        Assert.Equal(priority, registration.Priority);
        Assert.True(registration.IsEnabled);
        Assert.NotEqual(Guid.Empty, registration.Id);
    }
}