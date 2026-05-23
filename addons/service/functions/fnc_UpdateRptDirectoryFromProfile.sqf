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

private _profile = call FUNC(GetProfileConfiguration);
private _RPT_Directory = _profile getOrDefault ["RPT_Directory", ""];
TRACE_1("fnc_UpdateRptDirectory",_RPT_Directory);

if (_RPT_Directory isEqualTo "") exitWith {
  ERROR("""fnc_UpdateRptDirectoryFromProfile"" Exception : Missing required parameters (RPT_Directory)");
  nil
};

private _result = "DiscordMessageAPI" callExtension ["UpdateRptDirectory", [_RPT_Directory]];
_result params ["_return", "_returnCode"];
INFO_1("fnc_UpdateRptDirectoryFromProfile || Result : %1",_return);

if (_returnCode < 0) then {
  [_result # 1] call BIS_fnc_error;
};

nil
