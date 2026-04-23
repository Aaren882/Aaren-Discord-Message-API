#include "script_component.hpp"

/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_UpdateProfileIdentity
Description:
    Update Profile Identity to backend.

Parameters:
    _profileName          - The profile name to be updated <STRING>
    _serverInfoMessageId  - The message ID of the server info message to link the profile with <STRING>

Returns:
    <NONE>

Examples
    (begin example)
        ["MyServerProfile", "123123123"] call DiscordAPI_service_fnc_UpdateProfileIdentity
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */
if (!GVAR(Available)) exitWith {};

params [
  ["_profileName", call FUNC(GetProfileName), [""]],
  ["_messageId", "", [""]]
];

TRACE_1("fnc_UpdateProfileIdentity",_this);
if (_profileName isEqualTo "") exitWith {};

private _map = createHashMap;
_map set ["profileName", _profileName];
_map set ["MessageId", _messageId];

[_map, __UpdateServerIdentityExtension__] call FUNC(SendWebSocketJSON);
