#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_fnc_sendJsonFormat
Description:
    Send a JSON file with formatted string replacements.

Parameters:
    _file     - JSON file directory <STRING>
    _formats  - Array of [Key, Value] pairs for replacement <ARRAY>
    _Sel      - Webhook index <NUMBER>
    _payload  - Additional payload data like HandlerType and MessageID <ARRAY>

Returns:
    Extension return <ARRAY>

Examples
    (begin example)
        [
          "Server_Info_msg.json",
          [
            ["{MAP_NAME}", worldName]
          ],
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

TRACE_1("fn_sendJsonFormat",_this);


// Function Name: ["_file","_formats","_Sel"] call DiscordAPI_fnc_sendJsonFormat

params [
  ["_file", ""],
  "_formats",
  ["_Sel", DiscordMessageAPI_WebhookSel],
  ["_payload", nil]
];

if (isNil "_Sel" || _file == "" || isNil "_payload") exitWith {};

/* 
[
  //- both "Key" and "Value" are Strings
  
  ["Key", "Value"],
  ["{SERVER_IP}", "192.168.xx.xx"],
  ["{PLAYER_COUNT}", str (count allUsers)],
  ["Key", "Value"]
] 

//- Example of Sending JSON Payload
  [
    "Server_Info_msg.json",
    [],
    DiscordMessageAPI_ServerWebhookSel
  ] call DiscordAPI_fnc_sendJsonFormat;
*/

private _Info = "DiscordMessageAPI" callExtension [
  "ParseJson", 
  [//- File Directory
    _file
  ] 
];

private _url = DiscordEmbedBuilder_Info # 0 # _Sel;
private _msg = toString ((_Info # 0) call DiscordAPI_fnc_Deserialize_ExtensionOutput);

{
  _msg = [_msg,_x # 0,_x # 1] call CBA_fnc_replace;
} forEach _formats;

//- Struct hashMap
_payload = createHashMapFromArray _payload;
_payload set ["Url", _url];

//- Send Format Json
"DiscordMessageAPI" callExtension [ 
  "HandlerJsonFormat", 
  [
    toJSON _payload,
    _msg
  ] 
];
