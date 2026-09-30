#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_webhook_fnc_callExtension
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
        ["Refresh_Webhooks", [-1]] call DiscordAPI_webhook_fnc_callExtension
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params [["_paramName", "", [""]], ["_args", nil, [ [] ]]];

private _isV2 = "v2" in GVAR(ExtensionVersion);
TRACE_2("fnc_callExtension",_isV2,_this);

private _result = [GVAR(ExtensionVersion), _paramName, _args] call DiscordAPI_fnc_callExtension;

if (_result isEqualType []) then {
  _result params ["_message", "_returnCode"];
  
  //- Check extension exception
  if (_returnCode < 0) then {
    ERROR_2("Websocket ""Extension"" threw an exception: ""%1"". (IsV2=%2)",_message,_isV2);
    throw _message;
  };
};

_result
