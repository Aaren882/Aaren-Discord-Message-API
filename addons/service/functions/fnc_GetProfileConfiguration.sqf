#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_GetProfileConfiguration
Description:
    Retrieves the configuration data for the current server profile, including message IDs and file modification timestamps.
    This configuration is used to synchronize local template files with the backend service.

Parameters:
    None

Returns:
    _configData - An array containing the profile name and the JSON-serialized configuration map <ARRAY>

Examples
    (begin example)
        call DiscordAPI_service_fnc_GetProfileConfiguration
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

TRACE_1("fnc_GetProfileConfiguration",_this);

private _profile = GVAR(Profiles) call FUNC(GetProfile);

private _configuration = _profile getOrDefault ["Configuration", createHashMap];
if (count _configuration == 0) exitWith {
  ERROR_1("Cannot find ""MessageFile""=> ""%1"" from current profile !!",_configuration);
  nil
};

_profile
