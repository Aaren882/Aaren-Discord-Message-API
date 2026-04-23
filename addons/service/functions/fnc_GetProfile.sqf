#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_GetProfile
Description:
    Retrieves the server profile configuration from a specified JSON file.
    The profile contains settings such as message IDs and file paths that are 
    essential for establishing the connection and communication with the backend service.

Parameters:
    _profileFile - The name of the profile file to load -  <STRING>

Returns:
    _profile - The profile configuration data as a HashMap or Array -  <HASHMAP>

Examples
    (begin example)
        "default" call DiscordAPI_service_fnc_GetProfile
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params [
  ["_profileFile","default",[""]]
];

TRACE_1("fnc_GetProfile",_this);

private _file = format ["profiles/%1.json", _profileFile];
private _profile = fromJSON ([_file, []] call DiscordAPI_fnc_FormatJson);

_profile
