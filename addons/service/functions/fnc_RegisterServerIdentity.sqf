#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_RegisterServerIdentity
Description:
    Registers the server identity and information template to the backend.
    This function reads a JSON template file and sends both the profile identity 
    and the template content to the service via WebSocket.

Parameters:
    _profileName  - The profile name to be registered <STRING>
    _messageId    - The unique identifier for the message to be registered <STRING>
    _messageFile  - The path to the JSON template file <STRING>

Returns:
    <NONE>

Examples
    (begin example)
        ["MyServerProfile", "123123123", "Templates\ServerInfo.json"] call DiscordAPI_service_fnc_RegisterServerIdentity
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */
if (!GVAR(Available)) exitWith {};

params [
  ["_profileName", call FUNC(GetProfileName), [""]],
  ["_messageId", "", [""]],
  ["_messageFile", "", [""]]
];

TRACE_1("fnc_RegisterServerIdentity",_this);
if (_profileName isEqualTo "" || _messageFile isEqualTo "") exitWith {};

private _jsonContent = [_messageFile, []] call DiscordAPI_fnc_FormatJson;

private _map = createHashMap;
{
  _x params ["_payloadTitle", "_payloadValue"];
  private _m = createHashMap;
  
  _m set _payloadValue;
  _m set ["MessageId", _messageId]; //- Shared value

  //- put into the same result hashMap
  _map set [_payloadTitle, _m];

} forEach [
  ["Identity", ["profileName", _profileName]],
  ["InfoTemplate", ["JsonContent", _jsonContent]]
];


[_map, __RegisterServerIdentity__] call FUNC(SendWebSocketJSON);
