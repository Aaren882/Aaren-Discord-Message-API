#include "script_component.hpp"

INFO(MSG_INIT);

#include "XEH_PREP.hpp"

//- Version swap
[
  QGVAR(ExtensionVersion), "LIST", 
  [
    LLSTRING(ExtensionVersion),
    LLSTRING(ExtensionVersion_Tip)
  ],
  ["DiscordMessageAPI Settings", LOCAL_STR("Webhook")], 
  [["DiscordMessageAPI", "DiscordMessageAPIv2"], ["v1","v2"], 0],
  2
] call CBA_fnc_addSetting;
