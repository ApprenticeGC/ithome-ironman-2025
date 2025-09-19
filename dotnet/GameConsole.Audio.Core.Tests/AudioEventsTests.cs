using GameConsole.Audio.Core;
using System.Numerics;
using Xunit;

namespace GameConsole.Audio.Core.Tests;

/// <summary>
/// Tests for audio events and event handling.
/// </summary>
public class AudioEventsTests
{
    [Fact]
    public void AudioStartedEvent_Should_Implement_IAudioEvent()
    {
        // Act
        var eventType = typeof(AudioStartedEvent);

        // Assert
        Assert.True(typeof(IAudioEvent).IsAssignableFrom(eventType));
    }

    [Fact]
    public void AudioStartedEvent_Should_Initialize_Properties()
    {
        // Arrange
        var sourceId = "test-source";
        var timestamp = DateTime.UtcNow;
        var category = AudioCategory.Music;
        var audioPath = "music/background.mp3";
        var volume = 0.8f;
        var position = AudioPosition.ThreeD(new Vector3(1, 0, 0));

        // Act
        var audioEvent = new AudioStartedEvent
        {
            SourceId = sourceId,
            Timestamp = timestamp,
            Category = category,
            AudioPath = audioPath,
            Volume = volume,
            Position = position
        };

        // Assert
        Assert.Equal(sourceId, audioEvent.SourceId);
        Assert.Equal(timestamp, audioEvent.Timestamp);
        Assert.Equal(category, audioEvent.Category);
        Assert.Equal(audioPath, audioEvent.AudioPath);
        Assert.Equal(volume, audioEvent.Volume);
        Assert.Equal(position, audioEvent.Position);
    }

    [Fact]
    public void AudioStoppedEvent_Should_Implement_IAudioEvent()
    {
        // Act
        var eventType = typeof(AudioStoppedEvent);

        // Assert
        Assert.True(typeof(IAudioEvent).IsAssignableFrom(eventType));
    }

    [Fact]
    public void AudioStoppedEvent_Should_Initialize_Properties()
    {
        // Arrange
        var sourceId = "test-source";
        var timestamp = DateTime.UtcNow;
        var category = AudioCategory.SFX;
        var reason = AudioStopReason.Manual;
        var wasCompleted = false;

        // Act
        var audioEvent = new AudioStoppedEvent
        {
            SourceId = sourceId,
            Timestamp = timestamp,
            Category = category,
            Reason = reason,
            WasCompleted = wasCompleted
        };

        // Assert
        Assert.Equal(sourceId, audioEvent.SourceId);
        Assert.Equal(timestamp, audioEvent.Timestamp);
        Assert.Equal(category, audioEvent.Category);
        Assert.Equal(reason, audioEvent.Reason);
        Assert.Equal(wasCompleted, audioEvent.WasCompleted);
    }

    [Fact]
    public void AudioVolumeChangedEvent_Should_Implement_IAudioEvent()
    {
        // Act
        var eventType = typeof(AudioVolumeChangedEvent);

        // Assert
        Assert.True(typeof(IAudioEvent).IsAssignableFrom(eventType));
    }

    [Fact]
    public void AudioVolumeChangedEvent_Should_Initialize_Properties()
    {
        // Arrange
        var sourceId = "test-source";
        var timestamp = DateTime.UtcNow;
        var category = AudioCategory.Voice;
        var previousVolume = 0.5f;
        var newVolume = 0.8f;
        var isMasterVolume = true;

        // Act
        var audioEvent = new AudioVolumeChangedEvent
        {
            SourceId = sourceId,
            Timestamp = timestamp,
            Category = category,
            PreviousVolume = previousVolume,
            NewVolume = newVolume,
            IsMasterVolume = isMasterVolume
        };

        // Assert
        Assert.Equal(sourceId, audioEvent.SourceId);
        Assert.Equal(timestamp, audioEvent.Timestamp);
        Assert.Equal(category, audioEvent.Category);
        Assert.Equal(previousVolume, audioEvent.PreviousVolume);
        Assert.Equal(newVolume, audioEvent.NewVolume);
        Assert.Equal(isMasterVolume, audioEvent.IsMasterVolume);
    }

    [Fact]
    public void AudioPositionChangedEvent_Should_Implement_IAudioEvent()
    {
        // Act
        var eventType = typeof(AudioPositionChangedEvent);

        // Assert
        Assert.True(typeof(IAudioEvent).IsAssignableFrom(eventType));
    }

    [Fact]
    public void AudioPositionChangedEvent_Should_Initialize_Properties()
    {
        // Arrange
        var sourceId = "test-source";
        var timestamp = DateTime.UtcNow;
        var category = AudioCategory.Ambient;
        var previousPosition = AudioPosition.ThreeD(new Vector3(0, 0, 0));
        var newPosition = AudioPosition.ThreeD(new Vector3(5, 0, 0));

        // Act
        var audioEvent = new AudioPositionChangedEvent
        {
            SourceId = sourceId,
            Timestamp = timestamp,
            Category = category,
            PreviousPosition = previousPosition,
            NewPosition = newPosition
        };

        // Assert
        Assert.Equal(sourceId, audioEvent.SourceId);
        Assert.Equal(timestamp, audioEvent.Timestamp);
        Assert.Equal(category, audioEvent.Category);
        Assert.Equal(previousPosition, audioEvent.PreviousPosition);
        Assert.Equal(newPosition, audioEvent.NewPosition);
    }

    [Fact]
    public void AudioStateChangedEvent_Should_Implement_IAudioEvent()
    {
        // Act
        var eventType = typeof(AudioStateChangedEvent);

        // Assert
        Assert.True(typeof(IAudioEvent).IsAssignableFrom(eventType));
    }

    [Fact]
    public void AudioStateChangedEvent_Should_Initialize_Properties()
    {
        // Arrange
        var sourceId = "test-source";
        var timestamp = DateTime.UtcNow;
        var category = AudioCategory.UI;
        var previousState = AudioState.Loading;
        var newState = AudioState.Playing;
        var errorMessage = "Test error message";

        // Act
        var audioEvent = new AudioStateChangedEvent
        {
            SourceId = sourceId,
            Timestamp = timestamp,
            Category = category,
            PreviousState = previousState,
            NewState = newState,
            ErrorMessage = errorMessage
        };

        // Assert
        Assert.Equal(sourceId, audioEvent.SourceId);
        Assert.Equal(timestamp, audioEvent.Timestamp);
        Assert.Equal(category, audioEvent.Category);
        Assert.Equal(previousState, audioEvent.PreviousState);
        Assert.Equal(newState, audioEvent.NewState);
        Assert.Equal(errorMessage, audioEvent.ErrorMessage);
    }

    [Fact]
    public void AudioStateChangedEvent_Should_Allow_Null_ErrorMessage()
    {
        // Arrange
        var sourceId = "test-source";
        var timestamp = DateTime.UtcNow;
        var category = AudioCategory.SFX;
        var previousState = AudioState.Paused;
        var newState = AudioState.Playing;

        // Act
        var audioEvent = new AudioStateChangedEvent
        {
            SourceId = sourceId,
            Timestamp = timestamp,
            Category = category,
            PreviousState = previousState,
            NewState = newState,
            ErrorMessage = null
        };

        // Assert
        Assert.Equal(sourceId, audioEvent.SourceId);
        Assert.Equal(timestamp, audioEvent.Timestamp);
        Assert.Equal(category, audioEvent.Category);
        Assert.Equal(previousState, audioEvent.PreviousState);
        Assert.Equal(newState, audioEvent.NewState);
        Assert.Null(audioEvent.ErrorMessage);
    }

    [Theory]
    [InlineData(AudioStopReason.Completed)]
    [InlineData(AudioStopReason.Manual)]
    [InlineData(AudioStopReason.Interrupted)]
    [InlineData(AudioStopReason.Error)]
    [InlineData(AudioStopReason.ResourceLimit)]
    public void AudioStoppedEvent_Should_Support_All_Stop_Reasons(AudioStopReason stopReason)
    {
        // Act
        var audioEvent = new AudioStoppedEvent
        {
            SourceId = "test",
            Timestamp = DateTime.UtcNow,
            Category = AudioCategory.SFX,
            Reason = stopReason,
            WasCompleted = stopReason == AudioStopReason.Completed
        };

        // Assert
        Assert.Equal(stopReason, audioEvent.Reason);
        Assert.Equal(stopReason == AudioStopReason.Completed, audioEvent.WasCompleted);
    }

    [Theory]
    [InlineData(AudioState.Stopped)]
    [InlineData(AudioState.Playing)]
    [InlineData(AudioState.Paused)]
    [InlineData(AudioState.Loading)]
    [InlineData(AudioState.Error)]
    public void AudioStateChangedEvent_Should_Support_All_States(AudioState state)
    {
        // Act
        var audioEvent = new AudioStateChangedEvent
        {
            SourceId = "test",
            Timestamp = DateTime.UtcNow,
            Category = AudioCategory.Music,
            PreviousState = AudioState.Stopped,
            NewState = state,
            ErrorMessage = state == AudioState.Error ? "Test error" : null
        };

        // Assert
        Assert.Equal(state, audioEvent.NewState);
        if (state == AudioState.Error)
        {
            Assert.NotNull(audioEvent.ErrorMessage);
        }
    }

    [Theory]
    [InlineData(AudioCategory.SFX)]
    [InlineData(AudioCategory.Music)]
    [InlineData(AudioCategory.Voice)]
    [InlineData(AudioCategory.Ambient)]
    [InlineData(AudioCategory.UI)]
    public void All_AudioEvents_Should_Support_All_Categories(AudioCategory category)
    {
        // Act & Assert - Test that all events can use any category
        var startedEvent = new AudioStartedEvent { Category = category };
        Assert.Equal(category, startedEvent.Category);

        var stoppedEvent = new AudioStoppedEvent { Category = category };
        Assert.Equal(category, stoppedEvent.Category);

        var volumeEvent = new AudioVolumeChangedEvent { Category = category };
        Assert.Equal(category, volumeEvent.Category);

        var positionEvent = new AudioPositionChangedEvent { Category = category };
        Assert.Equal(category, positionEvent.Category);

        var stateEvent = new AudioStateChangedEvent { Category = category };
        Assert.Equal(category, stateEvent.Category);
    }

    [Fact]
    public void IAudioEvent_Should_Have_Required_Properties()
    {
        // Arrange
        var eventInterfaceType = typeof(IAudioEvent);

        // Act & Assert
        var sourceIdProperty = eventInterfaceType.GetProperty("SourceId");
        Assert.NotNull(sourceIdProperty);
        Assert.Equal(typeof(string), sourceIdProperty.PropertyType);

        var timestampProperty = eventInterfaceType.GetProperty("Timestamp");
        Assert.NotNull(timestampProperty);
        Assert.Equal(typeof(DateTime), timestampProperty.PropertyType);

        var categoryProperty = eventInterfaceType.GetProperty("Category");
        Assert.NotNull(categoryProperty);
        Assert.Equal(typeof(AudioCategory), categoryProperty.PropertyType);
    }
}