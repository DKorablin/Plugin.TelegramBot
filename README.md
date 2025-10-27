# Telegram Bot Host Plugin
[![Auto build](https://github.com/DKorablin/Plugin.TelegramBot/actions/workflows/release.yml/badge.svg)](https://github.com/DKorablin/Plugin.TelegramBot/releases/latest)

A lightweight, extensible Telegram Bot host framework that enables modular chat-processing through plugins. This library helps you build scalable Telegram bot solutions where each plugin independently handles its own message processing and reply logic.

## Features

- **Plugin System**
  - Automatic plugin discovery and loading
  - Per-plugin chat and message lifecycle management
  - Independent message handlers and reply logic for each plugin

- **Command Handling**
  - Built-in command and callback resolution
  - Automatic usage hints generation
  - Support for both text commands and inline callbacks

- **Reliability & Performance**
  - Automatic reconnection handling
  - Proxy support with random proxy selection
  - Rate limit handling per chat/user
  - In-memory caching system

- **Message Processing**
  - HTML sanitizing and safe formatting helpers
  - Message history tracking
  - Rich message formatting support

## Installation

1. Download one of the following host applications
2. Extract to a folder of your choice.
3. Copy the `Plugin.TelegramBot` to the `Plugins` folder of the host application.
4. Launch the host application.
5. Configure telegram bot settings in the host application settings:
  - BotToken: Your Telegram bot token
6. Check that the plugin is connected and running in the output logs or window.
7. Write your own plugins using [SAL.Interface.TelegramBot](https://github.com/DKorablin/SAL.Interface.TelegramBot) base libraries to connect them to telegram services using this plugin.

## Requirements

- .NET Framework 4.8 or .NET Standard 2.0
- Telegram.Bot package (automatically installed as dependency)
- One of the supported flatbed host applications:
  - [Flatbed.Dialog (Lite)](https://dkorablin.github.io/Flatbed-Dialog-Lite)
  - [Flatbed.Dialog](https://dkorablin.github.io/Flatbed-Dialog)
  - [Flatbed.MDI](https://dkorablin.github.io/Flatbed-MDI)
  - [Flatbed.MDI (Avalon)](https://dkorablin.github.io/Flatbed-MDI-Avalon)
  - [Flatbed.WorkerService](https://dkorablin.github.io/Flatbed-WorkerService)

## Support

If you encounter any issues or have questions, please [open an issue](https://github.com/DKorablin/Plugin.TelegramBot/issues) on GitHub.