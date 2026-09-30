#include "script_component.hpp"

private _infoVar = missionNamespace getVariable ["DiscordEmbedBuilder_Info",[]];
if (
  !hasInterface || 
  _infoVar findIf {true} < 0
) exitWith {};

systemChat str LOCAL_STR("Init_Hint");
["Init_Player", [_infoVar # 1]] call FUNC(callExtension);
