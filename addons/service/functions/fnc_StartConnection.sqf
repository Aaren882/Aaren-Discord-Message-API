#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_StartConnection
Description:
    Initiates the connection to the backend service by sending the necessary profile information and message content.
    This function is typically called during the mission startup sequence to establish communication with the backend.

Parameters:
    <NONE>

Returns:
    <NONE>

Examples
    (begin example)
        call DiscordAPI_service_fnc_StartConnection
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

TRACE_1("fnc_StartConnection",_this);

private _profile = GVAR(Profiles) call FUNC(GetProfile);

private _messageId = _profile getOrDefault ["MessageId", ""];
private _messageFile = _profile getOrDefault ["MessageFile", ""];

if (_messageFile isEqualTo "") exitWith {
  ERROR_1("Cannot find ""MessageFile""=> ""%1"" from current profile !!",_messageFile);
};

private _map = createHashMap;
_map set ["type", 2]; //- payload type "GameServer"
_map set ["MessageId", _messageId];
_map set ["MessageContent", [_messageFile, []] call DiscordAPI_fnc_FormatJson];

private _serverName = call FUNC(GetProfileName);
"DiscordMessageAPI" callExtension ["ConnectWebSocket",[_serverName, toJSON _map]];
