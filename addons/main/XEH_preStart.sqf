#include "script_component.hpp"

//- Must Be Multiplayer
if !(isDedicated) exitWith {};

//- Load Extension on startup to prepare game infos (e.g. RPT directory...)
"DiscordMessageAPI" callExtension "";
