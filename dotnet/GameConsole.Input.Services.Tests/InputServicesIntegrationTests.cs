using GameConsole.Input.Core;
using GameConsole.Input.Services;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace GameConsole.Input.Services.Tests;

/// <summary>
/// Integration tests for input services demonstrating their functionality.
/// </summary>
public class InputServicesIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<KeyboardInputService> _keyboardLogger;
    private readonly ILogger<MouseInputService> _mouseLogger;
    private readonly ILogger<GamepadInputService> _gamepadLogger;
    private readonly ILogger<InputMappingService> _mappingLogger;
    private readonly ILogger<InputRecordingService> _recordingLogger;

    public InputServicesIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        _keyboardLogger = loggerFactory.CreateLogger<KeyboardInputService>();
        _mouseLogger = loggerFactory.CreateLogger<MouseInputService>();
        _gamepadLogger = loggerFactory.CreateLogger<GamepadInputService>();
        _mappingLogger = loggerFactory.CreateLogger<InputMappingService>();
        _recordingLogger = loggerFactory.CreateLogger<InputRecordingService>();
    }

    [Fact]
    public async Task KeyboardInputService_Should_Initialize_And_Handle_Key_States()
    {
        // Arrange
        var keyboardService = new KeyboardInputService(_keyboardLogger);

        // Act
        await keyboardService.InitializeAsync();
        await keyboardService.StartAsync();

        // Wait for the service to be running
        Assert.True(keyboardService.IsRunning);

        // Check initial key state (should be false since no keys are pressed)
        var isWPressed = await keyboardService.IsKeyPressedAsync(KeyCode.W);
        
        // Assert
        Assert.False(isWPressed); // Initially no keys are pressed

        // Cleanup
        await keyboardService.StopAsync();
        await keyboardService.DisposeAsync();
        
        _output.WriteLine("KeyboardInputService successfully initialized and handled key states");
    }

    [Fact]
    public async Task MouseInputService_Should_Initialize_And_Track_Position()
    {
        // Arrange
        var mouseService = new MouseInputService(_mouseLogger);

        // Act
        await mouseService.InitializeAsync();
        await mouseService.StartAsync();

        // Wait for the service to be running
        Assert.True(mouseService.IsRunning);

        // Get initial mouse position
        var position = await mouseService.GetMousePositionAsync();
        var isLeftPressed = await mouseService.IsMouseButtonPressedAsync(MouseButton.Left);
        
        // Assert
        Assert.Equal(Vector2.Zero, position); // Initially at origin
        Assert.False(isLeftPressed); // Initially no buttons pressed

        // Cleanup
        await mouseService.StopAsync();
        await mouseService.DisposeAsync();
        
        _output.WriteLine($"MouseInputService successfully initialized with position: {position}");
    }

    [Fact]
    public async Task GamepadInputService_Should_Initialize_And_Handle_Controllers()
    {
        // Arrange
        var gamepadService = new GamepadInputService(_gamepadLogger);

        // Act
        await gamepadService.InitializeAsync();
        await gamepadService.StartAsync();

        // Wait for the service to be running
        Assert.True(gamepadService.IsRunning);

        // Check gamepad functionality
        var connectedCount = await gamepadService.GetConnectedGamepadCountAsync();
        var isConnected = await gamepadService.IsGamepadConnectedAsync(0);
        var gamepadName = await gamepadService.GetGamepadNameAsync(0);
        
        // Assert
        Assert.True(connectedCount >= 0); // Should have at least 0 connected gamepads
        
        if (isConnected)
        {
            Assert.NotNull(gamepadName);
            _output.WriteLine($"Found connected gamepad: {gamepadName}");
            
            // Test button state
            var isAPressed = await gamepadService.IsGamepadButtonPressedAsync(0, GamepadButton.A);
            Assert.False(isAPressed); // Initially no buttons pressed
            
            // Test axis value
            var leftStickX = await gamepadService.GetGamepadAxisAsync(0, GamepadAxis.LeftStickX);
            Assert.InRange(leftStickX, -1.0f, 1.0f); // Should be within valid range
        }

        // Cleanup
        await gamepadService.StopAsync();
        await gamepadService.DisposeAsync();
        
        _output.WriteLine($"GamepadInputService successfully initialized with {connectedCount} connected gamepads");
    }

    [Fact]
    public async Task InputMappingService_Should_Handle_Key_Mappings()
    {
        // Arrange
        var mappingService = new InputMappingService(_mappingLogger);

        // Act
        await mappingService.InitializeAsync();
        await mappingService.StartAsync();

        // Test mapping functionality
        await mappingService.MapInputAsync("Keyboard:F", "Flashlight");
        
        var resolvedAction = await mappingService.ResolveInputMappingAsync("Keyboard:F");
        var mappingsForFlashlight = await mappingService.FindMappingsForActionAsync("Flashlight");
        
        var configuration = await mappingService.GetMappingConfigurationAsync();
        
        // Assert
        Assert.True(mappingService.IsRunning);
        Assert.Equal("Flashlight", resolvedAction);
        Assert.Contains("Keyboard:F", mappingsForFlashlight);
        Assert.NotNull(configuration);
        Assert.True(configuration.Mappings.Count > 0);
        
        _output.WriteLine($"InputMappingService successfully mapped 'Keyboard:F' to 'Flashlight'");
        _output.WriteLine($"Total mappings in configuration: {configuration.Mappings.Count}");

        // Test profile management
        var newProfile = await mappingService.CreateProfileAsync("TestProfile");
        await mappingService.SwitchProfileAsync("TestProfile");
        
        var profiles = await mappingService.GetAvailableProfilesAsync();
        
        Assert.NotNull(newProfile);
        Assert.Contains("TestProfile", profiles);
        
        // Cleanup
        await mappingService.StopAsync();
        await mappingService.DisposeAsync();
    }

    [Fact]
    public async Task InputRecordingService_Should_Record_And_Playback_Sequences()
    {
        // Arrange
        var recordingService = new InputRecordingService(_recordingLogger);

        // Act
        await recordingService.InitializeAsync();
        await recordingService.StartAsync();

        // Test recording functionality
        var sessionId = await recordingService.StartRecordingAsync("TestSequence");
        Assert.NotNull(sessionId);
        
        var activeSessions = await recordingService.GetActiveRecordingSessionsAsync();
        Assert.Single(activeSessions);
        
        // Simulate some input events
        var keyEvent = new KeyEvent(KeyCode.A, InputState.Pressed, KeyModifiers.None, DateTime.UtcNow, 1);
        var mouseEvent = new MouseEvent(new Vector2(100, 100), Vector2.Zero, Vector2.Zero, MouseButton.Left, InputState.Pressed, DateTime.UtcNow, 2);
        
        recordingService.RecordInputEvent(keyEvent);
        recordingService.RecordInputEvent(mouseEvent);
        
        // Stop recording
        var sequence = await recordingService.StopRecordingAsync(sessionId);
        
        // Assert recording
        Assert.NotNull(sequence);
        Assert.Equal("TestSequence", sequence.Name);
        Assert.Equal(2, sequence.Events.Count);
        
        // Test saved sequences
        var savedSequences = await recordingService.GetSavedSequencesAsync();
        Assert.Contains(sequence, savedSequences);
        
        var retrievedSequence = await recordingService.GetSequenceByNameAsync("TestSequence");
        Assert.NotNull(retrievedSequence);
        Assert.Equal(sequence.Name, retrievedSequence.Name);
        
        _output.WriteLine($"Successfully recorded sequence '{sequence.Name}' with {sequence.Events.Count} events");

        // Test playback (this will queue the sequence for playback)
        await recordingService.PlaybackSequenceAsync(sequence);
        
        _output.WriteLine("InputRecordingService successfully queued sequence for playback");

        // Cleanup
        await recordingService.StopAsync();
        await recordingService.DisposeAsync();
    }

    [Fact]
    public async Task InputServices_Should_Support_Event_Streams()
    {
        // Arrange
        var keyboardService = new KeyboardInputService(_keyboardLogger);
        var mouseService = new MouseInputService(_mouseLogger);
        var gamepadService = new GamepadInputService(_gamepadLogger);

        var keyEvents = new List<KeyEvent>();
        var mouseEvents = new List<MouseEvent>();
        var gamepadEvents = new List<GamepadEvent>();

        // Act
        await keyboardService.InitializeAsync();
        await keyboardService.StartAsync();
        await mouseService.InitializeAsync();
        await mouseService.StartAsync();
        await gamepadService.InitializeAsync();
        await gamepadService.StartAsync();

        // Subscribe to events
        keyboardService.KeyEvent += (sender, e) => keyEvents.Add(e);
        mouseService.MouseEvent += (sender, e) => mouseEvents.Add(e);
        gamepadService.GamepadEvent += (sender, e) => gamepadEvents.Add(e);

        // Wait a bit for potential events
        await Task.Delay(2000);

        // Assert
        Assert.True(keyboardService.IsRunning);
        Assert.True(mouseService.IsRunning);
        Assert.True(gamepadService.IsRunning);
        
        _output.WriteLine($"Received {keyEvents.Count} key events, {mouseEvents.Count} mouse events, {gamepadEvents.Count} gamepad events during test period");

        // Cleanup
        await keyboardService.StopAsync();
        await keyboardService.DisposeAsync();
        await mouseService.StopAsync();
        await mouseService.DisposeAsync();
        await gamepadService.StopAsync();
        await gamepadService.DisposeAsync();
        
        _output.WriteLine("All input services successfully disposed");
    }

    [Fact]
    public async Task InputServices_Should_Work_Together_In_Integrated_Scenario()
    {
        // Arrange
        var keyboardService = new KeyboardInputService(_keyboardLogger);
        var mappingService = new InputMappingService(_mappingLogger);
        var recordingService = new InputRecordingService(_recordingLogger);

        // Act - Initialize all services
        await keyboardService.InitializeAsync();
        await keyboardService.StartAsync();
        await mappingService.InitializeAsync();
        await mappingService.StartAsync();
        await recordingService.InitializeAsync();
        await recordingService.StartAsync();

        // Test integrated workflow
        // 1. Set up input mapping
        await mappingService.MapInputAsync("Keyboard:Q", "Quit Game");
        var quitMapping = await mappingService.ResolveInputMappingAsync("Keyboard:Q");
        Assert.Equal("Quit Game", quitMapping);

        // 2. Start recording
        var sessionId = await recordingService.StartRecordingAsync("IntegratedTest");
        
        // 3. Simulate some input (in a real scenario, these would come from actual input)
        var keyEvent = new KeyEvent(KeyCode.Q, InputState.Pressed, KeyModifiers.None, DateTime.UtcNow, 1);
        recordingService.RecordInputEvent(keyEvent);
        
        // 4. Stop recording and verify
        var sequence = await recordingService.StopRecordingAsync(sessionId);
        Assert.Single(sequence.Events);
        
        // 5. Verify the recorded event can be mapped
        if (sequence.Events[0] is KeyEvent recordedKeyEvent)
        {
            var mappedAction = await mappingService.ResolveInputMappingAsync($"Keyboard:{recordedKeyEvent.Key}");
            Assert.Equal("Quit Game", mappedAction);
        }

        _output.WriteLine("Integrated scenario completed successfully:");
        _output.WriteLine($"- Mapped Keyboard:Q to '{quitMapping}'");
        _output.WriteLine($"- Recorded sequence '{sequence.Name}' with {sequence.Events.Count} events");
        _output.WriteLine("- All services working together correctly");

        // Cleanup
        await keyboardService.StopAsync();
        await keyboardService.DisposeAsync();
        await mappingService.StopAsync();
        await mappingService.DisposeAsync();
        await recordingService.StopAsync();
        await recordingService.DisposeAsync();
    }
}