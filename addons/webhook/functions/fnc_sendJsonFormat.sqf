#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_fnc_sendJsonFormat
Description:
    Sends a formatted JSON message to Discord using a webhook.
    The function takes a JSON string (or file content) and a payload containing 
    webhook information and message handling instructions.

Parameters:
    _msg      - JSON string or file content to be sent <STRING>
    _Sel      - Webhook index <NUMBER>
    _payload  - Additional payload data like HandlerType and MessageID <ARRAY>

Returns:
    Extension return <ARRAY>

Examples
    (begin example)
        [
          "{ ""JSON_KEY"" : ""JSON_VALUE"" }",
          0,
          [
            ["HandlerType", 0],
            ["MessageID", ""]
          ]
        ] call DiscordAPI_fnc_sendJsonFormat
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params [
  "_msg",
  ["_Sel", DiscordMessageAPI_WebhookSel],
  "_payload"
];

TRACE_1("fnc_sendJsonFormat",_this);

if (isNil "_Sel" || isNil "_payload") exitWith {
  ERROR_3("""fnc_sendJsonFormat"" Exception : ""_Sel"" = %1, ""_msg"" = %2, ""_payload"" = %3",_Sel,_msg,_payload);
};

private _url = DiscordEmbedBuilder_Info # 0 # _Sel;

//- Struct hashMap
_payload = createHashMapFromArray _payload;
_payload set ["Url", _url];

TRACE_1("fnc_sendJsonFormat (Send)",_payload);

//- Send Format Json
"DiscordMessageAPI" callExtension [ 
  "HandlerJsonFormat", 
  [
    toJSON _payload,
    _msg
  ] 
];
