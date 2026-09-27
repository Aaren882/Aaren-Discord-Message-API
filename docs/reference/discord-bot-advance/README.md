# 🤖 Discord Bot (Advance)

### ⚙️ Welcome

The `Arma3WebService` is the core engine that powers `DiscordMessageAPIService.dll` the bridge between the game server and Discord.

While the Arma 3 extension handles in-game logic, this service acts as the high-performance bridge to Discord, enabling advanced features like real-time WebSockets.

{% hint style="warning" %}
Please make sure you have certain degree of API/Networking knowledge.
{% endhint %}

### 🚀 What does it do?

Unlike basic webhook implementations, this backend provides a robust infrastructure for community management:

* **WebSocket Hub**: Maintains a persistent, bidirectional connection with your game server for instant communication.
* **In-game Management:** Remotely "_Broadcast messages /_ [_Admin commands (ServerHost)_](https://community.bistudio.com/wiki/serverCommandAvailable)_"_.
* **Log Handling**: Facilitates the streaming of RPT logs or downloading them directly to your staff channels.
* **Real-Time Game Monitoring**: Better customizations and more reliable status reports.

<div data-with-frame="true"><figure><img src="../../.gitbook/assets/image (4).png" alt="" width="563"><figcaption><p>Interactive Discord Message</p></figcaption></figure></div>

### :tools: Start hosting your own bot

{% content-ref url="hosting-bot.md" %}
[hosting-bot.md](hosting-bot.md)
{% endcontent-ref %}
