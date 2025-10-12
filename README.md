# Telegram messenger bot host plugin
[![Auto build](https://github.com/DKorablin/Plugin.TelegramBot/actions/workflows/release.yml/badge.svg)](https://github.com/DKorablin/Plugin.TelegramBot/releases/latest)

Lightweight extensible Telegram Bot host that loads multiple chat-processing plugins and routes incoming updates to them. Supports:
- Plugin discovery and per-plugin chat/message lifecycle management
- Command / callback resolution and usage hints generation
- Automatic reconnect & proxy (random proxy) integration
- HTML sanitizing / safe formatting helpers
- Per chat / user in-memory caching and rate limit handling

Use this library to build modular Telegram bot solutions where each plugin encapsulates its own message handlers and reply logic.