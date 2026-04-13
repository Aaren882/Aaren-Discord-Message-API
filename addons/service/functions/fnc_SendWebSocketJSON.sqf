#include "script_component.hpp"
#include "../JSONExtended.inc"

/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_SendWebSocketJSON
Description:
    Sends a JSON-formatted string to the backend service via WebSocket.
    The message is wrapped in a HashMap with a specified process type 
    and sent using the __JsonString__ discriminator.

Parameters:
    _content     - The JSON string to be sent <STRING>
    _processType - The internal processing type for the backend (default: 1) <NUMBER>

Returns:
    <NONE>

Examples
    (begin example)
        ["{""key"": ""value""}", 1] call DiscordAPI_service_fnc_SendWebSocketJSON
        ["[""[""Item1"",""Item2""]""]", 2] call DiscordAPI_service_fnc_SendWebSocketJSON
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params ["_content", ["_processType", 1, [0]]];
TRACE_1("fnc_SendWebSocketJSON",_this);

private _invalid = false;
private _map = createHashMap;
private _contentMap = fromJSON _content; //- Covert into hashMap object
_map set ["ProcessType", _processType];

switch (_processType) do {

  case __DiscrodSendExtension__: {
    _map set ["DiscordMessage", _contentMap];
  };

  case __ServerInfoExtension__: {
    _map set ["Infos", _contentMap];
  };

  default {
    _invalid = true
  };
};

//- Error
if (_invalid) exitWith {
  ERROR_1("""fnc_SendWebSocketJSON"" Exception : Invalid websocket message process type ""%1""",_processType);
};

[toJSON _map, __JsonString__] call FUNC(SendWebSocketMessage);
