---
description: >-
  The Discord Message API system is composed of several key components, each
  serving a distinct purpose in bridging Arma 3 with Discord.
---

# 🤓 Dev document

### Arma3WebService

The `Arma3WebService` is the backend service of the Discord Message API, acting as the central hub for Discord Bot integration, database management, and the WebSocket server.

* **Purpose**: It handles the core logic for interacting with Discord, managing server data, and providing administrative interfaces. It's responsible for integrating with the Discord Bot, managing the database, and running the WebSocket server for real-time communication.
* **Language**: C#.
* **Platform**: ASP.NET Core.
* **Key Functionalities**:
  * **Discord Bot Integration**: Manages the Discord bot's connection and interactions with Discord, including sending messages, handling commands, and managing permissions.
  * **Database Management**: Handles data persistence for configurations and other relevant information.
  * **WebSocket Server**: Facilitates real-time, bidirectional communication with the Arma 3 game server, allowing for instant updates and command execution.
  * **Configuration**: Configured primarily through environment variables or an `.env` file, with `appsettings.json` providing default values. This includes Discord bot token, database connection strings, service ports (WebSocket and Admin Console), and logging levels.

***

### Breakdown `DiscordMessageAPIService.dll`

The `DiscordMessageAPIService.dll` component acts as a crucial bridge between the Arma 3 game server and the `Arma3WebService` backend.

Its core methods are in `extension/ServiceConnection`.&#x20;

* **Purpose**: It's a DLL (Dynamic Link Library) that facilitates communication between the Arma 3 game engine (which uses SQF scripts) and the C# backend service. It handles data serialization and local service operations.
* **Language**: C#.
* **Platform**: _Windows **DLL**_ / _Linux **SO**_.
* **Key Functionalities**:
  * **Arma 3 ↔ Backend Bridge**: Enables Arma 3 to send data to and receive data from the `Arma3WebService`.
  * **Data Serialization**: Responsible for converting data between Arma 3's SQF format and the format expected by the C# backend.

***

## In-Game Scripts

The `addons` component refers to the client-side SQF scripts that run within Arma 3, providing the in-game interface and logic for the Discord Message API.

* **Purpose**: These scripts integrate the Discord Message API functionalities directly into Arma 3 missions and server operations. They handle mission logic, event handlers, and message formatting before data is sent to the backend.
* **Language**: SQF (Arma 3 Scripting Language).
* **Platform**: Arma 3.
* **Key Functionalities**:
  * **Mission Logic and Event Handlers**: SQF scripts are used to trigger Discord messages based on in-game events like mission start/end, player join/leave, or custom game events.
  * **Message Formatting**: Scripts prepare messages, often using JSON templates, before sending them to `DiscordMessageAPIService.dll`.
  * **Server Status Monitoring**: SQF functions can be called to periodically update server status messages on Discord, including player count, mission name, and server status.
  * **Sending Messages**: Provides functions for sending various types of messages:
    * `fnc_SendBotMessage`: For sending formatted Discord messages using templates.
    * `fnc_SendWebSocketJSON`: For sending raw JSON payloads.
    * `fnc_SendMessage`: For simple text messages.
    * `fnc_UpdateServerInfo`: For updating server status messages.
  * **Initialization**: The `service/XEH_preInit.sqf` and `XEH_postInit.sqf` scripts handle the initial setup, including defining variables, registering CBA settings, and establishing the WebSocket connection to the backend.
  * **Callback Handling**: `service/XEH_postInit.sqf` sets up an `ExtensionCallback` mission event handler to process responses from the `./extension/ServiceConnection`, categorizing them into text, RPT data, commands, or JSON strings.
  * **Webhook Configuration**: SQF scripts interact with `Webhooks.json` files, which store Discord webhook URLs, allowing messages to be directed to specific Discord channels.
