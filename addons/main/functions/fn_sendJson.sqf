#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_fnc_sendJson
Description:
    Send json format file.

Parameters:
    _file  - json file directory can be absolute <STRING>
    _sel  - Webhook index <Number>

Returns:
    ARRAY of Extension return <ARRAY>

Examples
    (begin example)
        ["Server_Info_msg.json"] call DiscordAPI_fnc_sendJson
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */
params ["_file",["_sel",DiscordMessageAPI_WebhookSel]];

TRACE_1("fn_sendJson",_this);

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
