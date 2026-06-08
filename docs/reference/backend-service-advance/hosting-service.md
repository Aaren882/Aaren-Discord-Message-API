---
description: >-
  This guide is specifically designed for game server hosters and administrators
  who need to deploy and maintain the Arma3WebService (the backend component)
  for the Discord Message API.
---

# 🪛 Hosting Service

## 🧠 Component Overview

The `Arma3WebService` is ASP.NET Core application that facilitates communication between your Arma 3 game server and Discord.

* **🔗WebSocket Server**: Maintains persistent connections with game servers for real-time data flow.
* :robot:**Discord Bot**: Handles message delivery, embeds, and interaction callbacks.
* **Admin Console**: A Discord interactive interface for remotely managing and monitoring logs.
* **Persistence Layer**: Stores server metadata and message templates.

***

### 🛠️ Requirements & Prerequisites

| Component    | Requirement                                   | Note                                      |
| ------------ | --------------------------------------------- | ----------------------------------------- |
| **OS**       | Windows Server / Windows 10+ / Linux / Docker | Supports Windows Service deployment.      |
| **Ports**    | Custom / 5048 (TCP)                           | Standard defaults for WebSocket           |
| **Database** | SQLite (Default)                              | Use SQL Server for multi-server clusters. |

***

### **🚀 Backend Deployment Steps**

{% stepper %}
{% step %}
### Networking & Firewall

The backend service must be reachable by the Arma 3 server via extension

* **Extension Types:**
  * `DiscordMessageAPIService(_x64).dll`
  * `DiscordMessageAPIService(_x64).so` for Linux
* **Inbound Port 5048**: Must be open to allow the game server to connect via _**WebSocket & http**_.
{% endstep %}

{% step %}
### Environment Variables

Hosters should primarily use the `.env` file for configuration. Create this file in the root directory of `Arma3WebService.exe`.

```dotenv
ASPNETCORE_ENVIRONMENT = "Production";
ASPNETCORE_HTTPS_PORTS = 7172; //- (Optional)
ASPNETCORE_HTTP_PORTS = 5048;
BotToken = "BOT_TOKEN";
MonitorChannel = "ChannelID";
AdminChannel = "ChannelID";
LoggingChannel = "ChannelID";
AdminLoggingChannel = "ChannelID";
AdminPassword = "in game AdminPassword"; //- (Optional but some remote functions won't be working)
```


{% endstep %}

{% step %}
### Hosting the service

For 24/7 uptime, do not run the console window manually. Use **PM2** to wrap the `Arma3WebService.exe` as a service.

{% tabs %}
{% tab title="Hosting on PM2 (Lightweight Alternative)" %}
**Requirement:**&#x20;

* Must have [Node.js](https://nodejs.org/) installed on the hosting machine.
* Make sure `ecosystem.config.js` is set up.

```powershell
# install pm2
npm install pm2 -g

# check pm2 is ready, type `pm2` in your terminal
pm2

# start up the service
pm2 start webServiceDirectory/ecosystem.config.js --name "Arma3WebService"

# start up background daemon to keep the service runing even after re-boot
pm2 startup
pm2 save
```
{% endtab %}

{% tab title="Hosting on Docker" %}
WIP
{% endtab %}
{% endtabs %}
{% endstep %}
{% endstepper %}

***

### 🗄️ Database Strategies

#### Single Server / Low Traffic

Keep `DATABASE_TYPE=Sqlite`. This stores all data in `./test.db`. It is highly efficient and requires zero maintenance.

#### Multi-Server / Hosting Providers

If you are hosting dozens of Arma 3 servers, use `DATABASE_TYPE=SqlServer`.

1. Point all backend instances to a centralized SQL Server.
2. Ensure the connection string in `appsettings.json` or `.env` is properly formatted.

***

### 🛡️ Security for Hosters

1. **Token Protection**: The `DISCORD_BOT_TOKEN` grants full access to your bot. Never place this in the Arma 3 mission files. It must live exclusively on the backend server environment.
2. **JWT Authentication**: The communication between the extension and the backend is secured via JWT (JSON Web Tokens). Ensure your `Arma3WebService` is reachable via HTTPS if traffic traverses the public internet.

***

### 📡 WebSocket Endpoint Logic

The backend exposes a specific endpoint for the game server: `ws://[YOUR-IP]:[SERVICE-PORT]/api/ws/ingame`

* **Binary Transfers**: Used for sending `.rpt` log or `.json` files from the server to the backend.
* **JSON Actions**: Standard message triggers (Mission Start, Player Join).
* **Callbacks**: Allows the backend to send commands back to the game server (e.g., triggering an in-game global hint from Discord).
