#include "script_component.hpp"

/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_UpdateService_ServerInfo
Description:
    Update Server Info to Discord.
    This function is called on Player Connected, Disconnected and Mission Started.

Parameters:
    _bypass  - Force update the message instead waiting for the timer. <BOOL>

Returns:
    <NONE>

Examples
    (begin example)
        [false] call DiscordAPI_service_fnc_UpdateService_ServerInfo
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */
params [["_bypass", false]];

TRACE_1("fnc_UpdateService_ServerInfo",_this);

//- Check Refresh time
if (
  !isServer || 
  ((time <= localNamespace getVariable ["DiscordAPI_ServerRefresh_Time",0]) && !_bypass)) exitWith {};

localNamespace setVariable ["DiscordAPI_ServerRefresh_Time", time + DiscordMsg_API_Delay];

private _infoList = call FUNC(GetServerInfo);

[_infoList apply {str _x}, __ArrayString__] call FUNC(SendWebSocketMessage);
