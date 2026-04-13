#include "script_component.hpp"

/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_SendWebSocketMessage
Description:
    Sends a structured message to the backend service via WebSocket.
    The message is categorized by a discriminator (e.g., Text, JsonString, ArrayString) 
    to ensure the backend processes the data correctly.

    "_discriminator" => #LINK - addons/service/MessageTypes.inc

Parameters:
    _content       - The data content to be sent (String or Array of Strings) <STRING/ARRAY/HASHMAP>
    _discriminator - The type identifier for the message (e.g., __Text__, __JsonString__, __ArrayString__) <NUMBER>

Returns:
    <NONE>

Examples
    (begin example)
        ["msg", 1] call DiscordAPI_service_fnc_SendWebSocketMessage
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params ["_content", "_discriminator"];
TRACE_1("fnc_SendWebSocketMessage",_this);

private _invalid = false;

private _map = switch (_discriminator) do {
  //- Use #LINK - addons/service/functions/fnc_SendWebSocketJSON.sqf
  case __JsonString__: {
    _content // #NOTE - _content needs to be Hashmap
  };
  case __Text__: {
    private _m = createHashMap;
    _m set ["Message", _content];

    _m
  };
  case __ArrayString__: { //- "[["",""],["",""]]"
    private _m = createHashMap;
    _m set ["ArrayString", _content];

    _m
  };
  default {
    _invalid = true;
    createHashMap
  };
};

//- Error
if (_invalid) exitWith {
  ERROR_1("Invalid websocket message discriminator ""%1""",_discriminator);
};


// toJSON
_map set ["type", _discriminator];
private _json = toJSON _map;

TRACE_1("WebSocket Message",_json);

"DiscordMessageAPI" callExtension ["SendWebSocketMessage", [_json]];

nil
