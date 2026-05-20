#include "script_component.hpp"

INFO(MSG_INIT);

//- Variables
GVAR(Available) = false;

//- #NOTE - Server-Side only
//- Functions
#include "XEH_PREP.hpp"

private _profileFileNames = "profiles" call DiscordAPI_fnc_GetPathFiles;
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
  1,
  FUNC(UpdateRptDirectoryFromProfile)
] call CBA_fnc_addSetting;

uiNamespace setVariable [QGVAR(profileFileNames), _profileFileNames];

INFO_1("DISCORD_API [PreInit] || Profiles : %1",_profileFileNames);
