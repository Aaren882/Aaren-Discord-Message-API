<p align="center">
   <a href="https://github.com/Aaren882/Aaren-Discord-Message-API/releases/latest">
      <img src="https://img.shields.io/github/v/release/Aaren882/Aaren-Discord-Message-API?label=Latest&color=blue&logo=github" >
   </a>
   <img src="https://img.shields.io/steam/size/3319624071?label=File%20Size&logo=steam" >
   <img src="https://img.shields.io/steam/views/3319624071?label=Steam%20Views&logo=steam" >
   <img src="https://img.shields.io/steam/subscriptions/3319624071?label=Steam%20Downloads&logo=steam" >
</p>

<p align="center">
   <a href="https://discord.gg/QYCuYpDBgf">
        <img src="https://img.shields.io/badge/Discord_Join-Here-blue?style=for-the-badge&logo=discord">
    </a>
</p>

# What Does it do ?
A powerful and lightweight Arma 3 Extension to bridge your game server with Discord. **<ins>BattlEye Compatible ✅</ins>**

### 🚀 Key Features
- **Live Server Monitoring**: Track server status with highly customizable formats.
- **RPT Log Integration**: Real-time RPT log monitoring and retrieval via secure connections.
- **ATAK Integration**: Seamlessly send pictures/data from ATAK (Integrated with **[Better CAS Environment](https://steamcommunity.com/sharedfiles/filedetails/?id=3319624071)**).
- **Flexible Messaging**: Support for simple text, Discord Embeds, and full **_JSON_** payloads.
- **Secure & Encrypted**: Built with webhook encryption and secure service interactions.
- **Real-time Interaction**: Powered by a robust WebSocket service for low-latency communication.

## 📖 Documentation [IMPORTANT]
- 📢 Discord Server : https://discord.gg/QYCuYpDBgf
- 📃 Documentation : [Gitbook](https://aarens-base.gitbook.io/aarens-base-docs/v/aarens-discord-message-api)

<hr>

# 🛠 Requirements
- Arma 3 Server must be running in **MP Mode**.
- The extension works **AFTER** the mission has started.

# Also Check out
- This project is a fork of [SQFDiscordEmbedBuilder](https://github.com/ConnorAU/SQFDiscordEmbedBuilder) by  [ConnorAU <img src="https://avatars.githubusercontent.com/u/15099385?v=4" width="16"/>](https://github.com/ConnorAU) 

# License
https://github.com/ConnorAU/SQFDiscordEmbedBuilder/blob/master/LICENSE


## 🧠 Architectural Description for Maintainers
### 📐System Diagram

This model reflects the roles of each component in the client-server architecture:

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

* **Client-Side** major components:
  *   **`Arma 3 Game World` (Source/Responder):** This component transmits telemetry and status reports about the game state to the system. It also receives commands and events initiated by the system (e.g., "Broadcast this message" or "Export `.rpt` log"). It does not initiate complex events via the Broker.
  *   **`DiscordMessageAPIService` (Entry Point):** This is the initial interface layer. It is the first component to receive unmanaged calls from the Game World via native entries (`UnmanagedCallersOnly`). It accepts the raw input and immediately delegates the processing to the Broker.
  *   **`ServiceConnection` (The Broker/Coordinator):** This is the central traffic controller. It receives events from the Game World, translates them, and coordinates the flow between the Client, the Backend, and the Rules Engine.
  *   **`ExtensionComponents` (The Rules Engine):** This framework layer contains the application's logic, data models, and service contracts. It provides the operational guidelines for how the Broker and the Backend Server should process incoming data.
  *   **`Persistent Data` (The Memory):** This stores the state and configuration data required by the Rules Engine and Backend Server to maintain consistent operation across sessions.

* **Server Backend:**
  *   **`Arma3WebService` (Backend Server):** This is the authoritative server layer. It exposes API endpoints and controllers that handle requests from the Client. It contains the core business logic, utilizes the `Arma3WebService.DBContext` for data persistence, and manages real-time communication via WebSockets.

* **External Enpoint:**
  *   **`Discord API` (External Destination):** This is the external messaging service endpoint. It is the destination for all messages sent by the Broker.

### 📝 TL;DR: System Architecture

This architecture uses a **Middleware pattern** to connect the live **Arma 3 Game World** with the external **Discord API**.

*   **Core Idea:** All communication is funneled through the **Broker/Coordinator (`ServiceConnection`)**.
*   **Execution Flow:**
    1.  The Game sends **Telemetry/Status** $\rightarrow$ **Entry Point**.
    2.  The Entry Point $\rightarrow$ **Broker**.
    3.  The Broker consults the **Rules Engine (`ExtensionComponents`)** for logic.
    4.  The Broker directs commands to the **Backend Server (`Arma3WebService`)** for execution.

**In short: The Broker acts as the traffic controller, mediating all data and command flow between the Game's Backend Server and the Discord API.**
