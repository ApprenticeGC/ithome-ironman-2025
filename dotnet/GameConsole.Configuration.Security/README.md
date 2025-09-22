# GameConsole Configuration Security

This package provides comprehensive configuration security and encryption capabilities for GameConsole applications, implementing RFC-013-02.

## Features

### 🔐 Encryption Support
- **AES-256-CBC encryption** for sensitive configuration data
- **Azure Key Vault integration** for enterprise key management
- **Local encryption** for development and testing scenarios
- **Key rotation** capabilities for enhanced security
- **Integrity validation** using HMAC

### 👥 Role-Based Access Control
- **Hierarchical role system**: Guest → User → PowerUser → Administrator → System
- **Path-based permissions** for fine-grained access control
- **Sensitive data protection** (automatic restrictions for passwords, secrets, keys, tokens)
- **Explicit permission overrides** for specific users and paths

### 📊 Comprehensive Audit Logging
- **Detailed operation tracking** (read, write, delete, access control changes, key rotation)
- **Risk level assessment** (Low, Medium, High, Critical)
- **Compliance reporting** with various output formats
- **User activity tracking** with session and IP address logging
- **Structured JSON logging** for high-risk operations

### 🏪 Secure Configuration Store
- **Encrypted storage** for sensitive configuration values
- **Metadata tracking** (creation time, modification history, tags)
- **Backup and restore** capabilities
- **Import/export** functionality
- **Integrity validation** across all stored configurations

## Quick Start

### 1. Basic Setup

```csharp
// Configure services
services.AddSingleton<IConfigurationEncryption, LocalConfigurationEncryption>();
services.AddSingleton<IConfigurationAccessControl, ConfigurationAccessControl>();
services.AddSingleton<IConfigurationAuditLogger, ConfigurationAuditLogger>();
services.AddSingleton<ISecureConfigurationStore, SecureConfigurationStore>();
```

### 2. Azure Key Vault Setup

```csharp
// Add Azure Key Vault configuration encryption
services.AddSingleton<IConfigurationEncryption, AzureKeyVaultConfigurationEncryption>();

// Configure in appsettings.json:
{
  "ConfigurationSecurity": {
    "AzureKeyVault": {
      "Uri": "https://your-keyvault.vault.azure.net/"
    },
    "DefaultKeyId": "config-encryption-key"
  }
}
```

### 3. Role-Based Access Control

```csharp
// Initialize access control
await accessControl.InitializeAsync();
await accessControl.StartAsync();

// Add users to roles
await accessControl.AddUserToRoleAsync("user1", ConfigurationRole.User);
await accessControl.AddUserToRoleAsync("admin1", ConfigurationRole.Administrator);

// Check permissions
bool canRead = await accessControl.CanReadConfigurationAsync("database:connectionstring", "user1");
bool canWrite = await accessControl.CanWriteConfigurationAsync("app:theme", "user1");
```

### 4. Secure Configuration Storage

```csharp
// Store sensitive configuration (encrypted)
await store.SetConfigurationAsync("database:password", "secretPassword", true, "admin1");

// Store non-sensitive configuration (plain text)
await store.SetConfigurationAsync("app:title", "My Application", false, "user1");

// Retrieve configuration (automatically decrypted if needed)
var password = await store.GetConfigurationAsync("database:password", "admin1");
```

### 5. Audit Logging

```csharp
// Audit entries are automatically logged by SecureConfigurationStore
// Generate compliance report
var report = await auditLogger.GenerateComplianceReportAsync(
    startDate: DateTimeOffset.UtcNow.AddDays(-30),
    endDate: DateTimeOffset.UtcNow,
    reportFormat: ReportFormat.Json
);

Console.WriteLine($"Total operations: {report.TotalEntries}");
Console.WriteLine($"Success rate: {report.SuccessfulOperations}/{report.TotalEntries}");
```

## Security Features

### 🛡️ FIPS Compliance Considerations
- Uses .NET's built-in cryptographic providers
- AES-256-CBC encryption algorithm
- HMAC-SHA256 for integrity validation
- Secure random number generation

### 🔑 Key Management
- **Azure Key Vault** integration for enterprise scenarios
- **Local key storage** for development
- **Key rotation** support with versioning
- **Multiple encryption keys** support

### 🚫 Access Restrictions
- **Sensitive path detection**: Automatically restricts access to paths containing "password", "secret", "key", "token", "connectionstring"
- **Role-based enforcement**: Only Administrator and System roles can access sensitive data
- **Explicit permissions**: Override role-based permissions for specific scenarios

### 📈 Risk Assessment
- **Automatic risk level determination** based on operation type and data sensitivity
- **High-risk operation alerting** with detailed JSON logging
- **Failed operation tracking** with elevated risk levels

## Architecture

The package follows GameConsole's 4-tier architecture:

- **Tier 1**: Core interfaces (`IConfigurationEncryption`, `IConfigurationAccessControl`, etc.)
- **Tier 2**: Base implementations with common functionality
- **Tier 3**: Concrete implementations (`LocalConfigurationEncryption`, `AzureKeyVaultConfigurationEncryption`)
- **Tier 4**: Provider-specific integrations (Azure Key Vault, etc.)

## Dependencies

- `Microsoft.Extensions.Configuration` (8.0.0)
- `Microsoft.Extensions.Logging` (8.0.1)
- `Azure.Security.KeyVault.Keys` (4.6.0)
- `Azure.Identity` (1.12.1)
- `GameConsole.Core.Abstractions` (project reference)

## Testing

The package includes comprehensive unit tests covering:
- ✅ Encryption/decryption roundtrip testing
- ✅ Role-based access control validation
- ✅ Audit logging functionality
- ✅ Error handling and edge cases
- ✅ Security boundary enforcement

Run tests:
```bash
dotnet test GameConsole.Configuration.Security.Tests
```

## License

This package is part of the GameConsole project. See the main repository for licensing information.