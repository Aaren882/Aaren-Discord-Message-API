private _infoVar = missionNamespace getVariable ["DiscordEmbedBuilder_Info",[]];
if (
  !hasInterface || 
  _infoVar findIf {true} < 0
) exitWith {};

systemChat str localize "STR_Discord_MSG_Init_Hint";
"DiscordMessageAPI" callExtension ["init_player",[_infoVar # 1]];