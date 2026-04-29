#include "script_component.hpp"

/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_UpdateServerInfoTemplate
Description:
    Updates the server information template on the backend service. 
    This function sends the specified message ID and configuration (including templates and actions) 
    to the backend via WebSocket to ensure the server info display is synchronized.

Parameters:
    _profileName          - The profile name to be updated <STRING>
    _serverInfoMessageId  - The message ID of the server info message to link the profile with <STRING>

Returns:
    <NONE>

Examples
    (begin example)
        ["123123123", [["MessageTemplate", ""],["MessageActions", ""]]] call DiscordAPI_service_fnc_UpdateServerInfoTemplate
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */
if (!GVAR(Available)) exitWith {};

params [
  ["_messageId", "", [""]],
  ["_configuration", createHashMap, [createHashMap]]
];

TRACE_1("fnc_UpdateServerInfoTemplate",_this);
if (
  _messageId isEqualTo "" ||
  _configuration getOrDefault ["MessageTemplate", ""] isEqualTo "" ||
  _configuration getOrDefault ["MessageActions", ""] isEqualTo ""
) exitWith {};

private _map = createHashMap;
_map set ["MessageId", _messageId];
_map set ["Configuration", _configuration];

[_map, __UpdateServerInfoExtension__] call FUNC(SendWebSocketJSON);
