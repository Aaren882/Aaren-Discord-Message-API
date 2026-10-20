#include "script_component.hpp"

//- Other Settings
[
  QGVAR(Connect_On_SP), "CHECKBOX", 
  [
    LLSTRING(connect_on_sp),
    LLSTRING(connect_on_sp_tooltip)
  ], 
  ["DiscordMessageAPI Settings", LLSTRING(setting_category)], 
  false,
  2
] call CBA_fnc_addSetting;

[
  QGVAR(Enable), "CHECKBOX", 
  [
    LLSTRING(enable),
    LLSTRING(enable_tooltip)
  ], 
  ["DiscordMessageAPI Settings", LLSTRING(setting_category)], 
  false,
  1,
  nil,
  true
] call CBA_fnc_addSetting;

//- #NOTE - Server-Side only
//- Functions
#include "XEH_PREP.hpp"
