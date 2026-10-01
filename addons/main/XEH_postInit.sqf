#include "script_component.hpp"


//- Must Be Multiplayer
#ifndef DEBUG_MODE_FULL
  if !(isMultiplayer) exitWith {};
#endif

//- initiate for Server only
if (isServer) then {

  //- Init on Mission Started
    ["CBA_settingsInitialized", {
      //- Fire postInit Event
      INFO(MSG_INIT);
      [QGVARMAIN(postInit_Server)] call CBA_fnc_LocalEvent;
      
      //- Start info loop
      INFO("Starting Server Monitoring Loop.");
      call DiscordAPI_fnc_ServerInfo_Loop;
      INFO("Server Monitoring Loop initialized and running.");
    }] call CBA_fnc_addEventHandler;

    //- Check Mission MPEnded (on Server Shutdown)
    0 spawn {
      INFO_1("Mission ""MPEnded"" Registering... (Dedicated Server: %1)",isDedicated);
      private _action = {
        INFO("Mission Unloading...");

        [QGVARMAIN(Mission_Unload_Server)] call CBA_fnc_LocalEvent;
        INFO("Mission Unloaded.");

        if (!isDedicated) then {
          INFO("Removing this ""Unload EventHandler""...");
          (this # 0) displayRemoveEventHandler [_thisEvent, _thisEventHandler];
          INFO("""Unload EventHandler"" Removed.");
        };
      };
      
      if (isDedicated) then {
        private _EndedEH = addMissionEventHandler ["MPEnded", _action];
        INFO_1("Mission ""MPEnded"" Registered. (ID: %1)",_EndedEH);
      } else {
        waitUntil { !isNull findDisplay 46 };
        findDisplay 46 displayAddEventHandler ["Unload", _action];
      };
    };
};
