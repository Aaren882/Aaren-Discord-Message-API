#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_fnc_ServerInfo_Loop
Description:
    Monitors server information by repeatedly calling the Discord API.

Parameters:
    <NONE>

Returns:
    <NONE>

Author:
    Aaren
---------------------------------------------------------------------------- */

INFO("Starting Server Monitoring Loop.");
[
  {
    [QGVARMAIN(ServerInfoLoop)] call CBA_fnc_LocalEvent;
    call DiscordAPI_fnc_ServerInfo_Loop;
  }, 
  [],
  DiscordMsg_API_Delay
] call CBA_fnc_waitAndExecute;
