# GameConsole UI Services

This directory contains the UI service implementations for the GameConsole 4-tier architecture.

## Key Components

- **ConsoleUIService**: Console-based implementation providing TUI functionality
  - Message display with color coding based on message type
  - Interactive menu display and selection
  - Console clearing and user input prompting
  - UTF-8 encoding support

## Features

- Colored console output for different message types
- Interactive menu navigation with numbered options
- Input validation and error handling
- Proper async lifecycle management following IService pattern
- Comprehensive logging support

## Usage

The ConsoleUIService is designed to be registered with the GameConsole service registry and follows the standard async service lifecycle:

1. `InitializeAsync()` - Sets up console encoding
2. `StartAsync()` - Marks service as running
3. Service operations - Display messages, menus, prompts
4. `StopAsync()` - Graceful shutdown
5. `DisposeAsync()` - Cleanup

## TUI-First Design

This implementation follows the "TUI-first" architecture principle, providing a rich terminal user interface that can be extended for more advanced scenarios.