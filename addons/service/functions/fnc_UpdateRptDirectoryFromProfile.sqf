#include "..\script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_UpdateRptDirectoryFromProfile
Description:
    Updates the RPT directory used by the DiscordMessageAPI extension based on the server profile configuration.
    This allows the extension to correctly locate the RPT file for processing and sending messages to Discord.

Parameters:
    NONE

Returns:
    NONE

Examples
    (begin example)
        call DiscordAPI_service_fnc_UpdateRptDirectoryFromProfile
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

private _result = ["UpdateRptDirectory", [GVAR(Profiles)]] call FUNC(callExtension);
_result params ["_return", "_returnCode"];

if (_returnCode < 0) then {
  ERROR_1("fnc_UpdateRptDirectoryFromProfile %1",_return);
  [_result # 1] call BIS_fnc_error;
} else {
  INFO_1("fnc_UpdateRptDirectoryFromProfile || Result : ""%1""",_return);
};

nil
