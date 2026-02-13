#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_fnc_Deserialize_ExtensionOutput
Description:
    Deserialize extension output ARRAY.

    🔶 New AOT extension 
    input string may look like this ==> "[[""A""], 123] --" 🪲

    in order to parse it into Array, this function do these 👇
    "[[""A""], 123] --"  =<FIX STRING>=>  "[[""A""], 123]"  =<Parse Array>=>  [[""A""], 123]

Parameters:
    _info  - ARRAY like STRING e.g."[""123"",567]" <STRING>

Returns:
    <ARRAY>

Examples
    (begin example)
        ["[""AA"",123] --"] call DiscordAPI_fnc_getExtensionOutput
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params ["_info"];
TRACE_1("fn_Deserialize_ExtensionOutput",_this);

private _output = reverse (_info # 0);
private _endString = count _output - (_output find ']'); 

_output = reverse _output;
_output =_output select [0, _endString];

parseSimpleArray _output;
