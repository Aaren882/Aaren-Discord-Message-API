params ["_file",["_sel",DiscordMessageAPI_WebhookSel]];

if (isNil{DiscordEmbedBuilder_Info}) exitWith {};

//- _file Format 
  // "D:\MyFolder\Message.json"
  // from current DLL Directory [ "Message.json" or "\MyFolder\Message.json" ]
  // Ex. ["Server_Info_msg.json"] call DiscordAPI_fnc_sendJson;

"DiscordMessageAPI" callExtension [ 
  "HandlerJson", 
  [
    [DiscordEmbedBuilder_Info # 0 # _sel,0],
    _file
  ]
];
