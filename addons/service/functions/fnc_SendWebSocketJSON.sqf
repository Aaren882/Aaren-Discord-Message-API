#include "script_component.hpp"
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
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params ["_content", ["_processType", 1, [0]]];
TRACE_1("fnc_SendWebSocketJSON",_this);

private _map = createHashMap;

_map set ["ProcessType", 1];
_map set ["JsonString", _content];

[_map, __JsonString__] call FUNC(SendWebSocketMessage);
