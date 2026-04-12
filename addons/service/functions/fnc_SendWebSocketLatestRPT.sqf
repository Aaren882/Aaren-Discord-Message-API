#include "script_component.hpp"

/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_SendWebSocketRPT
Description:
    Description.

Parameters:
    _param  - Parameter description <OBJECT>

Returns:
    Return description <NONE>

Examples
    (begin example)
        [params] call DiscordAPI_service_fnc_SendWebSocketRPT
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

private _rptDir = ("DiscordMessageAPI" callExtension ["SendWebSocketRPT", []]) # 0;
INFO_1("SendWebSocket RPT Directory : ""%1""",_rptDir);
