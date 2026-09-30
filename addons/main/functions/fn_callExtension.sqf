#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_fnc_callExtension
Description:
    Calls a function within a specified extension, handling parameters and error responses.

Parameters:
    _extension - The name of the extension/module to call.
    _paramName - The name of the function to execute within the extension.
    _args - Optional array of arguments to pass to the function.

Returns:
    The result returned by callExtension, or nil if an error is caught.

Author:
    Aaren
---------------------------------------------------------------------------- */

params [["_extension", ""], ["_paramName", "", [""]], ["_args", nil, [ [] ]]];
TRACE_1("fn_callExtension",_this);

try {
  if (_extension isEqualTo "") then {
    throw "Error: Missing parameters (_extension) in fnc_callExtension";
  };

  //- IF NONE "_args" RETURN
  if (isNil "_args") exitWith { _extension callExtension _paramName };

  private _result = _extension callExtension [_paramName, _args];
  _result params ["", "", "_errorCode"];

  private _errorMsg = switch (_errorCode) do {
    case 101: {"SYNTAX_ERROR_WRONG_PARAMS_SIZE"};
    case 102: {"SYNTAX_ERROR_WRONG_PARAMS_TYPE"};
    case 201: {"PARAMS_ERROR_TOO_MANY_ARGS"};
    case 301: {"EXECUTION_WARNING_TAKES_TOO_LONG"};
    case 400: {"EXTENSION_LOAD_FAILED"};
    case 403: {"EXTENSION_BLOCKED_BY_BATTLEYE"};
    case 404: {"EXTENSION_NOT_FOUND"};
    case 412: {"EXTENSION_BLOCKED_BY_SCRIPT"};
    case 415: {"EXTENSION_WRONG_ARCHITECTURE"};
    default {nil};
  };

  if (!isNil "_errorMsg") then {
    throw format ["Cannot call ""%1"" Extension (%2) | %3", _extension, _errorCode, _errorMsg];
  };

  _result;
} catch {
  ERROR_1("Main ""callExtension"" threw an exception: ""%1""",_exception);
  
  nil;
};

