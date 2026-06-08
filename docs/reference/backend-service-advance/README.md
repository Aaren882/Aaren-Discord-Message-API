# 🌐 Backend Service (Advance)

### ⚙️ Welcome to the **Backend Service** documentation

The `Arma3WebService` is the core engine that powers `DiscordMessageAPIService.dll`. While the Arma 3 extension handles in-game logic, this service acts as the high-performance bridge to Discord, enabling advanced features like real-time WebSockets and the Admin Console.

{% hint style="warning" %}
Please make sure you have certain degree of API/Networking knowledge
{% endhint %}

### 🚀 What is the Backend Service?

Unlike basic webhook implementations, this backend provides a robust infrastructure for community management:

* **WebSocket Hub**: Maintains a persistent, bidirectional connection with your game server for instant communication.
* **Rich Interactions**: Powers complex Discord features such as Modals, Buttons, and Select Menus.
* **Log Handling**: Facilitates the streaming of RPT logs and mission data directly to your staff channels.

<figure><img src="../../.gitbook/assets/image (4).png" alt="" width="563"><figcaption><p>Interactive Discord Message</p></figcaption></figure>

### :tools: Start hosting your own service

{% content-ref url="hosting-service.md" %}
[hosting-service.md](hosting-service.md)
{% endcontent-ref %}
