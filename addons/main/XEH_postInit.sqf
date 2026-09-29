#include "script_component.hpp"


//- Must Be Multiplayer
#ifndef DEBUG_MODE_FULL
  if !(isMultiplayer) exitWith {};
#endif

//- initiate for Server only
if (isServer) then {

  //- Init on Mission Started
    private _Info = "DiscordMessageAPIv2" callExtension ["Init_Server",[]]; //- Return webhooks counts
    private _Webhook = ((_Info # 0) call DiscordAPI_fnc_Deserialize_ExtensionOutput) + [_Info # 1];
    serverNamespace setVariable ["DiscordEmbedBuilder_Info", _Webhook];
    missionNamespace setVariable ["DiscordEmbedBuilder_Info", _Webhook,true];

    ["CBA_settingsInitialized", {
      //- Fire postInit Event
      INFO(MSG_INIT);
      [QGVARMAIN(postInit_Server)] call CBA_fnc_LocalEvent;
    }] call CBA_fnc_addEventHandler;

    //- Check Mission MPEnded (on Server Shutdown)
    0 spawn {
      INFO_1("Mission ""MPEnded"" Registering... (Dedicated Server: %1)",isDedicated);
      private _action = {
        INFO("Mission Unloading...");

        private _file = serverNamespace getVariable ["DiscordMessageAPI_ClosedJSON", ""];
        private _format = [];
        private _webhook_Sel = serverNamespace getVariable ["DiscordMessageAPI_ServerWebhookSel", ""];
        private _payload = [
          ["HandlerType", 1],
          ["MessageID", serverNamespace getVariable ["DiscordMessageAPI_ServerID", ""]]
        ];
        
        [
          [_file, _format] call DiscordAPI_fnc_FormatJson,
          _webhook_Sel,
          _payload
        ] call EFUNC(webhook,sendJsonFormat);

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
    
} else {
  //- Init Clients
  0 spawn {
    waitUntil {
      !isNil{DiscordEmbedBuilder_Info}
    };
    call DiscordAPI_fnc_init_player;
    [QGVARMAIN(postInit_Client)] call CBA_fnc_LocalEvent;
  };
};

//- initiate for Server and Client (for CBA Settings)
0 spawn {
  waitUntil {
    !isNil{DiscordEmbedBuilder_Info}
  };
  call DiscordAPI_fnc_refresh_webhooks;
};
