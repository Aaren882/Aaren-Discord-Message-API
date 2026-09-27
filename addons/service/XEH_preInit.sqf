#include "script_component.hpp"

INFO(MSG_INIT);

//- Variables
GVAR(Available) = false;

//- #NOTE - Server-Side only
//- Functions
#include "XEH_PREP.hpp"

private _profileFileNames = "profiles" call FUNC(GetPathFiles);
if (count _profileFileNames == 0) then
{
  ERROR("DISCORD_API [PreInit] || The 'profiles' directory is empty. Please ensure the 'profiles' folder exists and contains at least one profile. Restart the mission to scan again...");
} else {
  INFO_1("DISCORD_API [PreInit] || Profiles found ""%1"".",count _profileFileNames);
  [
    QGVAR(Profiles), "LIST", 
    [
      LLSTRING(profile)
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
};

//- Other Settings
[
  QGVAR(Connect_On_SP), "CHECKBOX", 
  [
    LLSTRING(connect_on_sp),
    LLSTRING(connect_on_sp_tooltip)
  ], 
  ["DiscordMessageAPI Settings", "Service"], 
  false
] call CBA_fnc_addSetting;
