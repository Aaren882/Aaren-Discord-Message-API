#include "script_component.hpp"

private _infoVar = missionNamespace getVariable ["DiscordEmbedBuilder_Info",[]];
if (
  !hasInterface || 
  _infoVar findIf {true} < 0
) exitWith {};

systemChat str LOCAL_STR("Init_Hint");

//- v2 uses different name
private _IsV2 = "v2" in GVAR(ExtensionVersion);
[
  ["init_player","Init_Player"] select _IsV2,
  [_infoVar # 1]
] call FUNC(callExtension);
