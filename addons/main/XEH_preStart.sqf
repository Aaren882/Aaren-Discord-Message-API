#include "script_component.hpp"

//- Must Be Multiplayer
#ifndef DEBUG_MODE_FULL
  if !(isDedicated) exitWith {};
#endif

//- Load Extension on startup to prepare game infos (e.g. RPT directory...)
"DiscordMessageAPI" callExtension "";
