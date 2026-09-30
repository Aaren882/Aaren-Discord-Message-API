#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_callExtension
Description:
    Calls an extension function within the Discord API module.

Parameters:
    _paramName  - The name of the extension function to call <STRING>
    _args       - The arguments for the extension function <ARRAY>

Returns:
    <STRING> - When "_args" is nil returns extension output as a string.
    <ARRAY<STRING,NUMBER,NUMBER>> - When "_args" is NOT nil returns structured data containing result details "[result, returnCode, errorCode]".
    <NONE> - When any exception is caught.

Examples
    (begin example)
        ["Refresh_Webhooks", [-1]] call DiscordAPI_service_fnc_callExtension
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params [["_paramName", "", [""]], ["_args", nil, [ [] ]]];
TRACE_1("fnc_callExtension",_this);

private _result = ["DiscordMessageAPIService", _paramName, _args] call DiscordAPI_fnc_callExtension;

if (_result isEqualType []) then {
  _result params ["_message", "_returnCode"];
  if (_returnCode < 0) then {
    ERROR_1("Service ""Extension"" threw an exception: ""%1""",_message);
    throw _message;
  };
};

_result
