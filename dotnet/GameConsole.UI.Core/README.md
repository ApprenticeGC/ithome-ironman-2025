# GameConsole UI Core

This directory contains the core UI abstractions and interfaces for the GameConsole 4-tier architecture.

## Key Components

- **IService**: Main UI service interface providing console-based user interface operations
- **UIModels**: Core UI data models including messages, menus, and menu items
- **Capability Interfaces**: Optional capabilities for message formatting and menu navigation

## Features

- Message display with different types (Info, Warning, Error, Success)
- Menu display and user selection
- Console clearing and input prompting
- Extensible capabilities for formatting and navigation

## Architecture

This follows the GameConsole 4-tier architecture:
- **Tier 1**: Stable contracts (IService interface)
- **Tier 2**: Capability interfaces for extensible behavior
- **Tier 3**: Core models and abstractions
- **Tier 4**: Implementation provided by GameConsole.UI.Services