#include "script_component.hpp"

/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_SendWebSocketJSON
Description:
    Sends a JSON-formatted string to the backend service via WebSocket.
    The message is wrapped in a HashMap with a specified process type 
    and sent using the __JsonString__ discriminator.

Parameters:
    _content     - The JSON string to be sent <HASHMAP>
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

params [["_contentMap", nil, [createHashMap]], ["_processType", __DiscordSendExtension__, [0]]];
TRACE_1("fnc_SendWebSocketJSON",_this);

_contentMap set ["ProcessType", _processType];
// private _invalid = false;
// private _map = createHashMap;

/* switch (_processType) do {

  case __DiscordSendExtension__: {
    _map set ["DiscordMessage", _contentMap];
  };

  case __UpdateServerIdentityExtension__: {
    _map set ["MessageId", _contentMap get "MessageId"];
    _map set ["serverInfoMessageId", _contentMap get "serverInfoMessageId"];
  };

  case __UpdateServerInfoExtension__: {
    _map set ["MessageId", _contentMap get "MessageId"];
    _map set ["JsonContent", _contentMap get "JsonContent"];
  };

  default {
    _invalid = true
  };
};

//- Error
if (_invalid) exitWith {
  ERROR_1("""fnc_SendWebSocketJSON"" Exception : Invalid websocket message process type ""%1""",_processType);
}; */

[toJSON _contentMap, __JsonString__] call FUNC(SendWebSocketMessage);
