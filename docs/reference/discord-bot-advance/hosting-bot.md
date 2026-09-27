---
description: >-
  This guide is specifically designed for game server hosters and administrators
  who need to deploy and maintain the Arma3WebService (the backend component)
  for the Discord Message API.
---

# 🪛 Hosting Bot

## 🧠 Component Overview

The `Arma3WebService` is ASP.NET Core application that facilitates communication between your Arma 3 game server and Discord.

* **🔗WebSocket Server**: Maintains persistent connections with game servers for real-time data flow.
* :robot:**Discord Bot**: Handles message delivery, embeds, and interaction callbacks.
* **Admin Console**: A Discord interactive interface for remotely managing and monitoring logs.
* **Persistence Layer**: Stores server metadata and message templates.

***

### 🛠️ Requirements & Prerequisites

| Component       | Requirement                                                         | Note                                      |
| --------------- | ------------------------------------------------------------------- | ----------------------------------------- |
| **Discord Bot** | [Create a Discord Bot](https://discord.com/developers/applications) | Obviously needs a bot.                    |
| **OS**          | Windows Server / Windows 10+ / Linux / Docker                       | Supports Windows Service deployment.      |
| **Ports**       | Custom / 5048 (TCP)                                                 | Standard defaults for WebSocket           |
| **Database**    | SQLite (Default)                                                    | Use SQL Server for multi-server clusters. |

***

### **🚀 Backend Deployment Steps**

{% stepper %}
{% step %}
### Networking & Firewall

The backend service must be reachable by the Arma 3 server via extension.

* **Extension Types:**
  * `DiscordMessageAPIService_x64.dll`
  * `DiscordMessageAPIService_x64.so` for Linux
* **Inbound Port 5048**: Must be open to allow the game server to connect via _**WebSocket & http**_.
{% endstep %}

{% step %}
### Configure Variables

{% hint style="info" %}
Rememeber to change secrets. (useful tool : [https://it-tools.tech/token-generator](https://it-tools.tech/token-generator))
{% endhint %}

{% tabs %}
{% tab title="Environment Variables" %}
Hosters should primarily use the `.env` file for configuration. Create this file at root directory of `Arma3WebService.exe`.

```dotenv
ASPNETCORE_ENVIRONMENT = "Production";
ASPNETCORE_HTTPS_PORTS = 7172; //- (Optional)
ASPNETCORE_HTTP_PORTS = 5048;

//- API Auth
Jwt_Secret = "SOMETHING SECURE DONT SHARE";
APIKey="SOMETHING SECURE DONT SHARE";

BotToken = "BOT_TOKEN";
MonitorChannel = "ChannelID";
AdminChannel = "ChannelID";
LoggingChannel = "ChannelID";
AdminLoggingChannel = "ChannelID";

AdminPassword = "in game AdminPassword"; //- (Optional but some remote functions won't be working)
```
{% endtab %}

{% tab title="Appsettings.json" %}
If you are hosting on a PC, you should find `appsettings.json` in the directory.\
It's similar to **Env Variables**, but has less precedence.

```json
{
  "BotToken": "BOT_TOKEN",
  "MonitorChannel": "ChannelID",
  "AdminChannel": "ChannelID",
  "LoggingChannel": "ChannelID",
  "AdminLoggingChannel": "ChannelID",
  "AdminPassword": "in game AdminPassword ",
  
  //- API Auth
  "APIKey": "SOMETHING SECURE DONT SHARE",
  
  "Jwt": {
    "Issuer": "issuer",
    "Audience": "audience",
    "Secret": "SOMETHING SECURE DONT SHARE"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  
  //- Setup Ports (SIMPLE)
  "HTTP_PORTS": "5048",
  "HTTPS_PORTS": "7172", // if you don't need https just remove it
  
  //- (ADVANCE)
  //- This will take precedence and override "HTTP_PORTS/HTTPS_PORTS" above
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://*:5048"
      },
      "Https": {
        "Url": "https://*:7172" 
      }
    }
  }
}

```
{% endtab %}
{% endtabs %}


{% endstep %}

{% step %}
### Hosting the service

For 24/7 uptime, do not run the console window manually. Use **PM2** to wrap the `Arma3WebService.exe` as a service.

{% tabs %}
{% tab title="Hosting on Docker" %}
#### Latest image

```bash
docker pull ghcr.io/aaren882/aaren-discord-message-api:latest
```

#### RC image (Release Candidate)

```bash
docker pull ghcr.io/aaren882/aaren-discord-message-api:rc
```

<p align="center"><a href="https://github.com/Aaren882/Aaren-Discord-Message-API/pkgs/container/aaren-discord-message-api" class="button primary" data-icon="docker">Image on github</a></p>

```yaml
services:
  arma3webservice:
    image: ghcr.io/aaren882/aaren-discord-message-api:latest
    ports:
      - "5048:5048"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_HTTP_PORTS=${HTTP_PORTS:-5048}
      
      # Useful Tool: https://it-tools.tech/token-generator
      - Jwt_Secret=${JWT_SECRET} 
      - APIKey=${API_Key}

      # (Database)
      - DB_PROVIDER=${DB_PROVIDER:-SQLite} # SQLite , MySQL , NpgSQL
      # SQLite
      # - DB_CONNECTION_STRING=Data Source=.data/data.db # ("Data Source=.data/data.db" is the default value)
      # MySQL (MySQL, mariaDB)
      # - DB_CONNECTION_STRING=Server=${DB_HOST:-localhost};Port=${DB_PORT:-3306};Database=${DB};User=${DB_USER};Password=${DB_PW}
      # PostgreSQL (NpgSQL)
      # - DB_CONNECTION_STRING=Host=${DB_HOST:-localhost};Port=${DB_PORT:-5432};Database=${DB};Username=${DB_USER};Password=${DB_PW}

      # (Discord)
      - BotToken=${BOT_TOKEN}
      - MonitorChannel=${MonitorChannel}
      - AdminChannel=${AdminChannel}
      - AdminLoggingChannel=${AdminLoggingChannel}
      - LoggingChannel=${LoggingChannel}

      # (IN GAME)
      # - AdminPassword=1234 # (Optional) Admin password for calling admin Commands
    
    volumes:
      - data:/app/.data # Persist data
      - profiles:/app/.profile # Client templates

volumes:
  data:
  profiles:
```
{% endtab %}

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
{% endtabs %}
{% endstep %}
{% endstepper %}

***

### 🛡️ Security for Hosters

1. **Token Protection**: The `BOT_TOKEN` grants full access to your bot. Never place this in the Arma 3 mission files. It must live exclusively on the backend server environment.
2. **Authentication Secrets**: The communication between the extension and the backend is secured via APIKey and JWT (JSON Web Tokens). Ensure your `Arma3WebService` is reachable via HTTPS if traffic traverses the public internet.
