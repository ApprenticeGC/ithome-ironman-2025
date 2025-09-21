# GameConsole Configuration Management System

This project implements RFC-013-01, providing comprehensive configuration management for the GameConsole system.

## Features

- **Centralized Configuration Management**: Single point of configuration access across all services
- **Multiple Configuration Sources**: Support for JSON, XML, and Environment Variables
- **Environment-Specific Resolution**: Automatic configuration inheritance and overrides
- **Schema Validation**: Comprehensive validation with DataAnnotations and custom rules
- **Hot-Reload Support**: Runtime configuration updates without restart
- **Event-Driven Notifications**: Configuration change events for reactive updates
- **Priority-Based Provider Chain**: Configurable loading order with override capabilities

## Architecture

The configuration system follows the 4-tier architecture pattern:

- **Tier 1**: Core abstractions and interfaces (`IConfigurationManager`, `IConfigurationProvider`)  
- **Tier 2**: Implementation classes (`ConfigurationManager`, `EnvironmentConfigurationResolver`)
- **Tier 3**: Provider implementations (`JsonConfigurationProvider`, `XmlConfigurationProvider`)
- **Tier 4**: External integrations (Microsoft.Extensions.Configuration)

## Key Components

### IConfigurationManager
Central interface for configuration management with lifecycle support:
- Configuration access and reloading
- Strongly-typed section binding
- Validation and change notifications

### Configuration Providers
Modular providers for different configuration sources:
- **JsonConfigurationProvider**: JSON file support with hot-reload
- **XmlConfigurationProvider**: XML file support with hot-reload  
- **EnvironmentVariablesConfigurationProvider**: Environment variable support

### Environment Resolution
Smart environment-specific configuration handling:
- Hierarchical configuration loading
- Environment inheritance (base → environment → mode → specific)
- Configurable override rules

### Validation System
Comprehensive validation framework:
- DataAnnotations support
- Custom validation rules
- JSON Schema validation (extensible)
- Detailed error reporting

## Usage Example

```csharp
// Configure services
services.AddGameConsoleConfiguration("Development", "Game");

// Use configuration manager
var configManager = serviceProvider.GetService<IConfigurationManager>();
await configManager.InitializeAsync();
await configManager.StartAsync();

// Get strongly-typed configuration
var gameConfig = await configManager.GetSectionAsync<GameConsoleConfiguration>("GameConsole");

// Listen for changes
configManager.ConfigurationChanged += (sender, args) => {
    Console.WriteLine($"Configuration changed: {args.SectionPath}");
};
```

## Configuration File Structure

The system supports hierarchical configuration loading:

1. `appsettings.json` - Base configuration
2. `appsettings.{Environment}.json` - Environment overrides  
3. `appsettings.{Mode}.json` - Mode-specific settings
4. `appsettings.{Environment}.{Mode}.json` - Combined overrides
5. Environment variables with prefix `GameConsole_`

## Environment Variable Support

Environment variables follow the pattern: `GameConsole_{Section}__{Property}`

Examples:
- `GameConsole_Engine__TargetFrameRate=144`
- `GameConsole_Audio__Volume=0.9`
- `GameConsole_ConnectionStrings__DefaultConnection="..."`

## Validation

The system supports multiple validation approaches:

```csharp
// DataAnnotations
public class EngineConfiguration
{
    [Required]
    public string Type { get; set; }
    
    [Range(30, 300)]
    public int TargetFrameRate { get; set; }
}

// Custom validation rules
validator.RegisterValidationRule<AudioConfiguration>(config => 
    config.Volume < 0 || config.Volume > 1.0 
        ? new ValidationResult("Volume must be between 0.0 and 1.0")
        : ValidationResult.Success);
```

## Testing

Comprehensive test coverage includes:
- Configuration manager lifecycle
- Provider behavior and priority ordering
- Environment resolution logic
- Validation system functionality
- Hot-reload and change notifications

Run tests with: `dotnet test GameConsole.Configuration.Core.Tests`

## Dependencies

- **Microsoft.Extensions.Configuration**: Core configuration framework
- **Microsoft.Extensions.Logging**: Logging integration  
- **GameConsole.Core.Abstractions**: Base service interfaces

## Integration

This configuration system integrates with:
- RFC-001-01: Base service interfaces and lifecycle
- RFC-003-03: Configuration-based service binding
- Future RFCs requiring configuration management