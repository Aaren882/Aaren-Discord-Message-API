
#include "..\script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_GetCurrentRptFilePath
Description:
    Retrieves the absolute file path of the current RPT (Report) file being used by the Arma 3 server.
    #NOTE - This function is used from DEBUG build

Parameters:
    NONE

Returns:
    _filePath - The absolute file path of the current RPT file <STRING>

Examples
    (begin example)
        call DiscordAPI_service_fnc_GetCurrentRptFilePath
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

private _result = "DiscordMessageAPI" callExtension ["GetCurrentRpt", []];
_result params ["_return", "_returnCode"];

if (_returnCode < 0) then {
  ERROR(_return);
  _return call BIS_fnc_error;
};

_return
