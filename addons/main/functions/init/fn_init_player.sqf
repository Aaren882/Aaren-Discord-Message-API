#include "script_component.hpp"

private _infoVar = missionNamespace getVariable ["DiscordEmbedBuilder_Info",[]];
if (
  !hasInterface || 
  _infoVar findIf {true} < 0
) exitWith {};

systemChat str localize "STR_Discord_MSG_Init_Hint";
"DiscordMessageAPI" callExtension ["Init_Player",[_infoVar # 1]];
