#include "script_component.hpp"

INFO(MSG_INIT);

//- Functions for Server-Side only
#include "XEH_PREP.hpp"

//- Variables
GVAR(Available) = false;

private _profileFileNames = "profiles" call DiscordAPI_fnc_GetPathFiles;
[
  QGVAR(Profiles), "LIST", 
  [
    "Profiles"
  ], 
  ["DiscordMessageAPI Settings", "Service"], 
  [
    _profileFileNames apply { (_x splitString ".") # 0 },
    _profileFileNames,
    0
  ],
  1
] call CBA_fnc_addSetting;
