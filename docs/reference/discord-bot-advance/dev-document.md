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
  * **Arma 3 ↔ Backend Bridge**: Enables Arma 3 to send data to and receive data from `Arma3WebService`.
  * **Data Serialization**: Responsible for converting data between Arma 3's SQF format and the format expected by the C# backend.

## :bar\_chart: Overview:

```mermaid
graph TD
    subgraph EXTERNAL_SERVICES
        A[Arma 3 Game World]
        D[(Discord API)]
    end

    subgraph APPLICATION_CLIENT
        C[DiscordMessageAPIService \n Entry-Point]
        SC[ServiceConnection \n The-Broker]
    end

    subgraph APPLICATION_BACKEND
        AW[Arma3WebService \n Backend-Server]
        DB_AW[(Arma3WebService.DBContext)]
    end

    subgraph CORE_LOGIC
        EC[ExtensionComponents \n Rules-Engine]
        E[(Persistent Data/Memory)]
    end

    %% --- Relationships ---
    
    %% 1. Game World calls the Entry Point (Initial Contact)
    A -->|1. Sends Telemetry/Status| C
    
    %% 2. Broker takes over orchestration
    C -->|2. Delegates Request/Event| SC
    
    %% 3. Broker consults Rules
    SC -->|3. Queries Rules/Context| EC
    SC -->|4. Queries Data| E
    
    %% 4. Broker communicates with Backend Server
    SC -->|5. Sends API/WS Request| AW
    AW -->|6. Reads/Writes State| DB_AW
    
    %% 5. External I/O
    AW -->|7. Sends Messages| D
    D -->|8. Sends Commands/Messages| AW
    
    %% 6. Command Response Loop
    AW -->|9. Sends Response/Data| SC
    SC -->|10. Routes Response/Command| A

    %% --- Styling ---
    style A fill:#e0f7fa,stroke:#00bcd4,stroke-width:2px,color:#000
    style D fill:#e0f7fa,stroke:#00bcd4,stroke-width:2px,color:#000
    style C fill:#f0f4c3,stroke:#aed581,stroke-width:2px,color:#000
    style SC fill:#ffeb3b,stroke:#ff9800,stroke-width:3px,color:#000
    style AW fill:#ffb74d,stroke:#f57c00,stroke-width:3px,color:#000
    style EC fill:#c8e6c9,stroke:#4caf50,stroke-width:2px,color:#000
    style E fill:#cfd8dc,stroke:#607d8b,stroke-width:2px,color:#000
    style DB_AW fill:#ffc,stroke:#333,stroke-width:1px,color:#000

    %% Key Flow Descriptions
    linkStyle 0 stroke:green,stroke-width:2px;
    linkStyle 1 stroke:darkred,stroke-width:2px;
    linkStyle 2 stroke:blue,stroke-width:2px;
    linkStyle 3 stroke:blue,stroke-width:2px;
    linkStyle 4 stroke:purple,stroke-width:2px;
    linkStyle 5 stroke:purple,stroke-width:2px;
    linkStyle 6 stroke:darkred,stroke-width:2px;
    linkStyle 7 stroke:orange,stroke-width:2px;
    linkStyle 8 stroke:orange,stroke-width:2px;
    linkStyle 9 stroke:orange,stroke-width:2px;
```
