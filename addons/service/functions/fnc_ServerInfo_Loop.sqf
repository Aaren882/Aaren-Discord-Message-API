#include "script_component.hpp"

[
  {
    [] call FUNC(UpdateService_ServerInfo);
    // [] call EFUNC(webhook,Update_ServerInfo);
    call FUNC(ServerInfo_Loop);
  }, 
  [],
  DiscordMsg_API_Delay
] call CBA_fnc_waitAndExecute;
