#include "script_component.hpp"

//- Must Be Multiplayer
if !(isMultiplayer) exitWith {};

//- initiate for Server only
if (isServer) then {

  //- Init on Mission Started
    private _Info = "DiscordMessageAPI" callExtension ["Refresh_Webhooks",[-1]];

    private _Webhook = (parseSimpleArray (_Info # 0)) + [_Info # 1];
    serverNamespace setVariable ["DiscordEmbedBuilder_Info", _Webhook];
    missionNamespace setVariable ["DiscordEmbedBuilder_Info", _Webhook,true];
    call DiscordAPI_fnc_ServerInfo_Loop;

  //- on Server Shutdown
    0 spawn {
      waitUntil { !isNull findDisplay 46 };

      //- Check Mission Ended
      findDisplay 46 displayAddEventHandler ["Unload",
      {
        private _msg = toString parseSimpleArray (("DiscordMessageAPI" callExtension [ 
          "ParseJson", 
          [//- File Directory
            serverNamespace getVariable ["DiscordMessageAPI_ClosedJSON", ""]
          ] 
        ]) # 0);

        with serverNamespace do {
          "DiscordMessageAPI" callExtension [ 
            "HandlerJsonFormat", 
            [
              [DiscordEmbedBuilder_Info # 0 # DiscordMessageAPI_ServerWebhookSel, 1, DiscordMessageAPI_ServerID],
              _msg
            ] 
          ];
        };

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
