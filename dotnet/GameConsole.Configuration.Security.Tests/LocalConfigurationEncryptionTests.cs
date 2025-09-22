using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using GameConsole.Configuration.Security;

namespace GameConsole.Configuration.Security.Tests;

/// <summary>
/// Unit tests for LocalConfigurationEncryption implementation.
/// </summary>
public class LocalConfigurationEncryptionTests
{
    private readonly Mock<ILogger<LocalConfigurationEncryption>> _mockLogger;
    private readonly LocalConfigurationEncryption _encryption;

    public LocalConfigurationEncryptionTests()
    {
        _mockLogger = new Mock<ILogger<LocalConfigurationEncryption>>();
        _encryption = new LocalConfigurationEncryption(_mockLogger.Object);
    }

    [Fact]
    public async Task EncryptAsync_ValidPlainText_ReturnsEncryptedData()
    {
        // Arrange
        var plainText = "test configuration value";
        
        // Act
        var result = await _encryption.EncryptAsync(plainText);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Data);
        Assert.NotEmpty(result.IV);
        Assert.Equal("default", result.KeyId);
        Assert.NotEmpty(result.HMAC);
        Assert.Equal("AES-256-CBC", result.Algorithm);
        Assert.Equal(1, result.Version);
        Assert.True(result.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task DecryptAsync_ValidEncryptedData_ReturnsOriginalPlainText()
    {
        // Arrange
        var plainText = "test configuration value";
        var encryptedData = await _encryption.EncryptAsync(plainText);
        
        // Act
        var decryptedText = await _encryption.DecryptAsync(encryptedData);
        
        // Assert
        Assert.Equal(plainText, decryptedText);
    }

    [Fact]
    public async Task ValidateIntegrityAsync_ValidEncryptedData_ReturnsTrue()
    {
        // Arrange
        var plainText = "test configuration value";
        var encryptedData = await _encryption.EncryptAsync(plainText);
        
        // Act
        var isValid = await _encryption.ValidateIntegrityAsync(encryptedData);
        
        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateIntegrityAsync_CorruptedData_ReturnsFalse()
    {
        // Arrange
        var plainText = "test configuration value";
        var encryptedData = await _encryption.EncryptAsync(plainText);
        
        // Corrupt the data
        encryptedData.Data[0] ^= 0xFF;
        
        // Act
        var isValid = await _encryption.ValidateIntegrityAsync(encryptedData);
        
        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task EncryptAsync_EmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _encryption.EncryptAsync(""));
    }

    [Fact]
    public async Task EncryptAsync_NullString_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _encryption.EncryptAsync(null!));
    }

    [Fact]
    public async Task DecryptAsync_NullEncryptedData_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _encryption.DecryptAsync(null!));
    }

    [Fact]
    public async Task RotateKeyAsync_ValidKeyIds_CompletesSuccessfully()
    {
        // Arrange
        var oldKeyId = "default";
        var newKeyId = "new-key";
        
        // Act & Assert
        await _encryption.RotateKeyAsync(oldKeyId, newKeyId);
        
        // Verify new key is available
        var availableKeys = await _encryption.GetAvailableKeysAsync();
        Assert.Contains(newKeyId, availableKeys);
    }

    [Fact]
    public async Task GetAvailableKeysAsync_ReturnsDefaultKey()
    {
        // Act
        var keys = await _encryption.GetAvailableKeysAsync();
        
        // Assert
        Assert.NotEmpty(keys);
        Assert.Contains("default", keys);
    }

    [Fact]
    public async Task EncryptDecrypt_WithCustomKeyId_WorksCorrectly()
    {
        // Arrange
        var plainText = "sensitive data";
        await _encryption.RotateKeyAsync("default", "custom-key");
        
        // Act
        var encryptedData = await _encryption.EncryptAsync(plainText, "custom-key");
        var decryptedText = await _encryption.DecryptAsync(encryptedData);
        
        // Assert
        Assert.Equal(plainText, decryptedText);
        Assert.Equal("custom-key", encryptedData.KeyId);
    }

    [Theory]
    [InlineData("simple text")]
    [InlineData("")]
    [InlineData("Text with special characters: !@#$%^&*()")]
    [InlineData("Unicode: 你好世界 🌍")]
    [InlineData("Very long text that spans multiple blocks and should test the encryption algorithm's ability to handle larger data sets without issues")]
    public async Task EncryptDecrypt_VariousInputs_PreservesData(string input)
    {
        // Skip empty string test as it throws ArgumentException
        if (string.IsNullOrEmpty(input))
            return;
            
        // Act
        var encryptedData = await _encryption.EncryptAsync(input);
        var decryptedText = await _encryption.DecryptAsync(encryptedData);
        
        // Assert
        Assert.Equal(input, decryptedText);
    }
}