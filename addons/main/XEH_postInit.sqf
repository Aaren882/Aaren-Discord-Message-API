#include "script_component.hpp"

//- Must Be Multiplayer
#ifndef DEBUG_MODE_FULL
  if !(isMultiplayer) exitWith {};
#endif

//- initiate for Server only
if (isServer) then {

  //- Init on Mission Started
    private _Info = "DiscordMessageAPI" callExtension ["Refresh_Webhooks",[-1]];

    private _Webhook = ((_Info # 0) call DiscordAPI_fnc_Deserialize_ExtensionOutput) + [_Info # 1];
    serverNamespace setVariable ["DiscordEmbedBuilder_Info", _Webhook];
    missionNamespace setVariable ["DiscordEmbedBuilder_Info", _Webhook,true];
    call DiscordAPI_fnc_ServerInfo_Loop;

  //- on Server Shutdown
    0 spawn {
      waitUntil { !isNull findDisplay 46 };

      //- Check Mission Ended
      findDisplay 46 displayAddEventHandler ["Unload",
      {

        private _file = serverNamespace getVariable ["DiscordMessageAPI_ClosedJSON", ""];
        private _format = [];
        private _webhook_Sel = serverNamespace getVariable ["DiscordMessageAPI_ServerWebhookSel", ""];
        private _payload = [
          ["HandlerType", 1],
          ["MessageID", serverNamespace getVariable ["DiscordMessageAPI_ServerID", ""]]
        ];
        
        [
          _file,
          _format,
          _webhook_Sel,
          _payload
        ] call DiscordAPI_fnc_sendJsonFormat;

        (this # 0) displayRemoveEventHandler [_thisEvent, _thisEventHandler];
      }];
    };

  //- Check Server Entry
    addMissionEventHandler ["PlayerConnected", {
      [true] call DiscordAPI_fnc_Update_ServerInfo;
    }];
    addMissionEventHandler ["HandleDisconnect", {
      [true] call DiscordAPI_fnc_Update_ServerInfo;
    }];

} else {
  //- Init Clients
  0 spawn {
    waitUntil {
      !isNil{DiscordEmbedBuilder_Info}
    };
    call DiscordAPI_fnc_init_player;
  };
};

//- initiate for Server and Client (for CBA Settings)
0 spawn {
  waitUntil {
    !isNil{DiscordEmbedBuilder_Info}
  };
  call DiscordAPI_fnc_refresh_webhooks;
};
