#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_AddCBASettings
Description: 
    Dynamically adds CBA settings for service profiles on the client side.
    This is used to synchronize the available profile list from the server to an admin client, 
    allowing them to select and manage service profiles via the CBA settings menu.
    #NOTE - "Client-Side" ,so this this shouldn't be executed on Server

Parameters:
    _profileFileNames - A list of available profile filenames retrieved from the server <ARRAY>

Returns:
    None

Examples:
    (begin example)
        [["default.json", "hardcore.json"]] call DiscordAPI_service_fnc_AddCBASettings
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params ["_profileFileNames"];
TRACE_1("fnc_AddCBASettings",_this);

[
  QGVAR(Profiles), "LIST", 
  [
    "Service Profile"
  ], 
  ["DiscordMessageAPI Settings", "Service"], 
  [
    _profileFileNames apply { (_x splitString ".") # 0 },
    _profileFileNames,
    0
  ],
  1
] call CBA_fnc_addSetting;
