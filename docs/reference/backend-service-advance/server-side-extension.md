# 📤 Server-Side Extension

{% hint style="warning" %}
`DiscordMessageAPISerivce.dll` is a **server-side** extension.

Make sure the mod is load via `-servermod` instead of `-mod` to **bypass BattEye whitelist**.
{% endhint %}

First, navigate to the directory of `arma3server_x64.exe`.\
There should be `Discord_Message_API` folder (if no, create a new one), that's where the configs are.

<figure><img src="../../.gitbook/assets/image (8).png" alt=""><figcaption><p>Setup Example</p></figcaption></figure>

## 🎮Configure `secret.json`

{% code title="./Discord_Message_API/secret.json" %}
```json
{
  "ServiceUri" : "http://localhost:5048", //- `https://` for SSL/TLS
  "WebSocketServiceUri" : "ws://localhost:5048/api/ws/ingame", //-  `wss://` for SSL/TLS
  "RPT_Directory": "C:/Users/MyUser/AppData/Local/Arma 3", //- Default RPT Directory
  "Secret" : { //- API Endpoint Auth
    "UserName" : "admin",
    "Password" : "password"
  }
}
```
{% endcode %}

***

### 👥Configure Profile (Optional)

This must be in `./profiles` folder that can be changed in `Addons Settings`.

{% code title="./Discord_Message_API/profiles/default.json" %}
```json
{
  //- "Configuration" can be emply e.g. "Configuration": {}
  "Configuration": {
    "MessageTemplate": "Bot/Server_Info_msg_old.json", //- (OPTIONAL) Directory to the json file
    "MessageOfflineTemplate": "Offline_msg.json"       //- (OPTIONAL)
  },
  "RPT_Directory": "C:/Users/MyUser/AppData/Local/Arma 3" //- (OPTIONAL) will fallback to `secret.json`
}
```
{% endcode %}

***

## Message template format

The format still follows the same as [Webhook's](https://aarens-base.gitbook.io/aarens-base-docs/aarens-discord-message-api/~/revisions/RInZDjt1JSGaqqpLQGxf/reference/reference/setup-server-monit/customize-server-info).

But you can have more customizations, e.g. `components`.

{% hint style="warning" %}
Don't use `ComponentsV2` for server monitor template.

* It will make the message not editable.
* Some fields will not be accepted, ex. `Embeds`.
{% endhint %}

<figure><img src="../../.gitbook/assets/image.png" alt="" width="375"><figcaption><p>Template Example</p></figcaption></figure>

<details>

<summary>📫Monitor Template</summary>

````json
{
  "embeds": [
    {
      "title": "🛰️ {SERVER_NAME} | Status Details",
      "description": "### Mission: `{MISSION_NAME}`\n**Status:** :white_check_mark: Server Online",
      "color": "3447003",
      "fields": [
        {
          "name": "📍 General Info",
          "value": "> **Map:** {MAP_NAME}\n> **Version:** {GAME_VERSION}\n> **Players:** {PLAYER_COUNT} / {AVALIABLE_PLAYERS}",
          "inline": true
        },
        {
          "name": "📊 Performance",
          "value": "**Server FPS:** `{SERVER_FPS}` (Min: {FPS_MIN})\n**Active Scripts:** `{ACTIVE_SCRIPTS}`",
          "inline": true
        },
        {
          "name": "🌐 Connection Details",
          "value": "```fix\narma.server.address:2302\n```",
          "inline": false
        },
        {
          "name": "👥 Player List",
          "value": "```\n{PLAYER_LIST}\n```",
          "inline": true
        },
        {
          "name": "⚡ Network Stats",
          "value": "```\n{PLAYER_NETWORK}\n```",
          "inline": true
        },
        {
          "name": "🛠️ Player Status",
          "value": "```\n{PLAYER_STATE}\n```",
          "inline": true
        }
      ],
      "footer": {
        "text": "Last updated: {SYSTEM_DATE} {SYSTEM_TIME}",
        "icon_url": "https://i.imgur.com/410CmKi.png"
      }
    }
  ],
  "components": [
    {
      "type": 1,
      "components": [
        {
          "type": 2,
          "style": 5,
          "label": "JOIN NOW",
          "url": "https://arma.abc.com"
        }
      ]
    }
  ]
}

````



</details>

<details>

<summary>⭕Offline Template</summary>

```json
{
  "embeds": [
    {
      "title": "⚠️ Mission Stopped",
      "description": "The system has detected that the service has been interrupted. Administrators, please check the backend status as soon as possible.",
      "color": "15548997",
      "fields": [
        {
          "name": "Current Status",
          "value": "🔴 Offline",
          "inline": true
        }
      ],
      "footer": {
        "text": "System under automatic monitoring • ⛔ Server OFFLINE",
        "icon_url": "https://i.imgur.com/410CmKi.png"
      },
      "timestamp": "true"
    }
  ]
}

```



</details>
