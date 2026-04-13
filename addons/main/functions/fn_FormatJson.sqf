#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_fnc_FormatJson
Description:
    Formats a JSON file by replacing placeholders with provided values.
    Placeholders in the JSON file should be in the format {KEY}.

    #NOTE - From current DLL Directory [ "Message.json" or "\MyFolder\Message.json" ]
      absolute directory is also acceptable ("D:\MyFolder\Message.json")
      Ex. ["Server_Info_msg.json"] call DiscordAPI_fnc_sendJson;

Parameters:
    _file    - JSON file directory can be absolute <STRING>
    _formats - Array of [Key, Value] pairs for replacement <ARRAY>

Returns:
    Formatted JSON string <STRING>

Examples
    (begin example)
        [
          "Server_Info_msg.json",
          [
            ["Key", "Value"]
          ]
        ] call DiscordAPI_fnc_FormatJson;
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params [
  "_file",
  ["_formats",[]]
];

TRACE_1("fn_FormatJson",_this);

if (isNil "_file") exitWith {
  ERROR_1("""fn_sendJsonFormat"" Exception : ""_file"" = %1",_file);
};

/* 
[
  //- both "Key" and "Value" are Strings
  
  ["Key", "Value"],
  ["SERVER_IP", "192.168.xx.xx"],
  ["PLAYER_COUNT", str (count allUsers)],
  ["Key", "Value"]
]
*/

private _Info = "DiscordMessageAPI" callExtension [
  "ParseJson", 
  [ //- File Directory
    _file
  ] 
];

// private _msg = toString ((_Info # 0) call DiscordAPI_fnc_Deserialize_ExtensionOutput);

private _msg = _Info # 0;
{
  _msg = [_msg, format ["{%1}", _x # 0], _x # 1] call CBA_fnc_replace;
} forEach _formats;

//- Return structured string
_msg
