#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_SendBotMessage
Description:
    Sends a formatted JSON message to Discord via the backend bot service.
    This function reads a template file, formats it, and transmits it through 
    the established WebSocket connection using the standard Discord message process type

Parameters:
    _paramFile    - The path to the JSON template file <STRING>
    _messageId    - The unique identifier for the message to be updated (optional) <STRING>

Returns:
    <NONE>

Examples
    (begin example)
        ["Templates\MyMessage.json", "123456789"] call DiscordAPI_service_fnc_SendBotMessage
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

if (!GVAR(Available)) exitWith {};

params [
  ["_messageFile", "", [""]],
  ["_messageId", "", [""]]
];

TRACE_1("fnc_SendBotMessage",_this);

private _content = [
  _messageFile,
  []
] call DiscordAPI_fnc_FormatJson;

private _map = createHashMap;
_map set ["DiscordMessage", fromJSON _content];

//- Optional message ID for update scenarios
if (_messageId isNotEqualTo "") then {
  _map set ["MessageId", _messageId];
};

[_map, __DiscordSendExtension__] call FUNC(SendWebSocketJSON);
