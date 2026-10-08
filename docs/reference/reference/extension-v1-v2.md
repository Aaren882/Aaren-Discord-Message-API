---
description: Two different extensions for your own demand.
---

# 🍭 Extension v1/v2

You can select different versions in **Addon Settings**.

The addon has backward compatibility with v1.\
**Server** and **Client** can be set to different versions.

{% hint style="info" %}
Please make sure you:

* Restart the mission if it's on server side.

To make sure the extension initialized properly.
{% endhint %}

***

#### There are two versions of `DiscordMessageAPI(v2)_x64`.

* **v1:** (`.NET Framework 4.8.1`)
  * The old version is very slow and resource-consuming.
  * Unlimited growing log files.
  * :eye: BattEye :white\_check\_mark:.
* **v2:** (`.NET 9 AOT`)
  * Very fast and efficient by comparing v1 "(around 82.20x faster, small GC)"
  * Supports Linux (`.so`).
  * Auto log file pruning.
  * :eye: BattEye :x: YET (I already sent it to them).

{% hint style="warning" %}
## For v2 user:

Since v2 is [AOT(Ahead Of Time)](https://hackmd.io/@0xdeveloperuche/H1h8vUbNeg) Complilation.

* All the logs and configuration have new home now 🏡.\
  \> :star:`Discord_Message_API` folder by the `arma3server_x64.exe` or `arma3_x64.exe`.
* :red\_circle: If you cannot access the `Discord_Message_API` for some reasons.\
  \> You can rollback to v1 for your needs.
{% endhint %}
