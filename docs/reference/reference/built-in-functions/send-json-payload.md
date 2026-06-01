# 📦 Send JSON Payload

```sqf
[
    _file, //- String
    _Sel //- Index of Webhook
] call DiscordAPI_fnc_sendJson;
```

***

## Quick Example 🐊

```sqf
[
    "Server_Ended_msg.json",
    0
] call DiscordAPI_fnc_sendJson;
```
