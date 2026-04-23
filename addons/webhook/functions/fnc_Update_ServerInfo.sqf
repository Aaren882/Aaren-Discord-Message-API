#include "script_component.hpp"

/* ----------------------------------------------------------------------------
Function: DiscordAPI_webhook_fnc_Update_ServerInfo
Description:
    Update Server Info to Discord.
    This function is called on Player Connected, Disconnected and Mission Started.

Parameters:
    _bypass  - Force update the message instead waiting for the timer. <BOOL>

Returns:
    <NONE>

Examples
    (begin example)
        [false] call DiscordAPI_webhook_fnc_Update_ServerInfo
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */
params [["_bypass", false]];

TRACE_1(QFUNC(fnc_Update_ServerInfo),_this);

private _messageID = serverNamespace getVariable ["DiscordMessageAPI_ServerID", ""];

//- Check Refresh time
if (
  !isServer || 
  _messageID == "" ||
  ((time <= localNamespace getVariable ["DiscordAPI_ServerRefresh_Time",0]) && !_bypass)) exitWith {};

localNamespace setVariable ["DiscordAPI_ServerRefresh_Time", time + DiscordMsg_API_Delay];

private _infoList = EFUNC(service,GetServerInfo);

//- Payload with Message ID
private _payload =  [
  ["HandlerType", 1],
  ["MessageID", _messageID]
];

//- Exit with Shutdown Msg
if (_infoList isEqualTo []) exitWith {
  private _file = serverNamespace getVariable ["DiscordMessageAPI_ClosedJSON", ""];
  private _format = [];
  private _webhook_Sel = serverNamespace getVariable ["DiscordMessageAPI_ServerWebhookSel", ""];
  
  [
    [_file, _format] call DiscordAPI_fnc_FormatJson,
    _webhook_Sel,
    _payload
  ] call FUNC(sendJsonFormat);
};

private _JSON_String = [
  serverNamespace getVariable ["DiscordMessageAPI_ServerJSON", ""],
  _infoList
] call DiscordAPI_fnc_FormatJson;

[
  _JSON_String,
  DiscordMessageAPI_ServerWebhookSel,
  _payload
  /* *** Payload Format ***
    - [0] Send by default
    - [1, "Message ID"] Refresh
  */
] call FUNC(sendJsonFormat);
