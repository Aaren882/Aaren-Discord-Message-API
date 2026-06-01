---
description: Simple tool to monit Server INFO.
---

# 🖥️ Setup Server-Monit

{% hint style="danger" %}
Server must under **MP** mode.

and it only works <mark style="color:yellow;">**AFTER**</mark> Mission Started.
{% endhint %}

## Step 1 - Select Webhook

You can find the options in 👇

**`"DiscordMessageAPI Settings" >> "Select the Server INFO Webhook"`**

Select Webhook with Index.

<figure><img src="../../../.gitbook/assets/image.png" alt=""><figcaption><p>CBA Setting</p></figcaption></figure>

***

## Step 2 - Send Message via API

So we can get the **`Message ID`**

_Paste the Code below into_ [<mark style="color:orange;">**Debug Console**</mark>](https://community.bistudio.com/wiki/Arma_3:_Debug_Console) 👇

{% code fullWidth="false" %}
```sqf
[  
    DiscordMessageAPI_ServerWebhookSel,
    "IM SERVER INFO (COPY MY ID)"
] call DiscordAPI_fnc_sendMessage;
```
{% endcode %}

{% hint style="warning" %}
Make sure the message is sent by **the same Webhook**.

So the Webhook can edit the messge.
{% endhint %}

***

## Step 3 - **`Message ID`**

Paste the **`Message ID`** that just sent.

<figure><img src="../../../.gitbook/assets/image (1).png" alt=""><figcaption><p>Settings for Server INFO</p></figcaption></figure>

{% hint style="info" %}
If the server have **Persistent** checked, the game will keep updating **Server-Status** even there's zero player in the server.
{% endhint %}

***

## Step 4 - Press _**"OK"**_&#x20;

<figure><img src="../../../.gitbook/assets/image (2).png" alt=""><figcaption><p>Press "OK" to Save</p></figcaption></figure>

{% hint style="info" %}
The basic setup is done !!

Make sure everything is on the rail ,then you can go down :arrow\_down:
{% endhint %}

{% content-ref url="customize-server-info.md" %}
[customize-server-info.md](customize-server-info.md)
{% endcontent-ref %}
