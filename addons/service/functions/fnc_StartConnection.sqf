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

private _profile = call FUNC(GetProfileConfiguration);

private _messageId = _profile getOrDefault ["MessageId", ""];
private _configuration = _profile getOrDefault ["Configuration", createHashMap];

private _map = createHashMap;
_map set ["type", 2]; //- payload type "GameServer"
_map set ["MessageId", _messageId];

private _dateTimes = "DiscordMessageAPI" callExtension [
  "GetDirectoryFilesDateTime",
  values _configuration
];

//- Get DateOffset in UNIX format
_map set [
  "ProfileDateOffsets",
  _dateTimes call DiscordAPI_fnc_Deserialize_ExtensionOutput
];

"DiscordMessageAPI" callExtension ["ConnectWebSocket", [call FUNC(GetProfileName), toJSON _map]];
