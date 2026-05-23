#include "script_component.hpp"

/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_SendWebSocketRPT
Description:
    Sends the latest RPT file to the backend service via WebSocket.
    The extension identifies the most recent RPT file in the server's log directory and initiates the transfer.

Parameters:
    None

Returns:
    _rptDir - The directory path of the RPT file being sent <STRING>

Examples:
    (begin example)
        call DiscordAPI_service_fnc_SendWebSocketLatestRPT
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

private _rptDir = ("DiscordMessageAPI" callExtension ["SendWebSocketRPT", []]) # 0;
INFO_1("SendWebSocket RPT Directory : ""%1""",_rptDir);

_rptDir
