#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_webhook_fnc_sendJson
Description:
    Send json format file.

Parameters:
    _file - json file directory can be absolute <STRING>
    _sel  - Webhook index <Number>

Returns:
    ARRAY of Extension return <ARRAY>

Examples
    (begin example)
        ["Server_Info_msg.json"] call DiscordAPI_webhook_fnc_sendJson
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */
params ["_file",["_sel",DiscordMessageAPI_WebhookSel]];

TRACE_1("fnc_sendJson",_this);

if (isNil{DiscordEmbedBuilder_Info}) exitWith {
  ERROR("""fnc_sendJson"" Exception : DiscordEmbedBuilder_Info is not defined.");
};

"DiscordMessageAPI" callExtension [ 
  "HandlerJson", 
  [
    [DiscordEmbedBuilder_Info # 0 # _sel,0],
    _file
  ]
];
