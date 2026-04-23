#include "script_component.hpp"

[
  "DiscordMsg_API_Delay", "TIME", 
  [
    LOCAL_STR("ServerInfo_Delay"),
    LOCAL_STR("ServerInfo_Delay_Tip")
  ], 
  ["DiscordMessageAPI Settings", LOCAL_STR("Server_INFO")],
  [3, 30, 5],
  1
] call CBA_fnc_addSetting;

[
  "DiscordMsg_API_isPersistent", "CHECKBOX", 
  [
    LOCAL_STR("isPersistent"),
    LOCAL_STR("isPersistent_Tip")
  ], 
  ["DiscordMessageAPI Settings", LOCAL_STR("Server_INFO")], 
  false,
  1
] call CBA_fnc_addSetting;
