#include "script_component.hpp"


//- Must Be Multiplayer
#ifndef DEBUG_MODE_FULL
  if !(isMultiplayer) exitWith {};
#endif

//- initiate for Server only
if (isServer) then {

  //- Init on Mission Started
    private _ServerName = serverName;
    _ServerName = [
      _ServerName,
      format ["SP_Server %1", call CBA_fnc_createUUID]
    ] select (_ServerName == "");

    private _Info = "DiscordMessageAPI" callExtension ["Init_Server",[_ServerName]]; //- Return webhooks counts

    private _Webhook = ((_Info # 0) call DiscordAPI_fnc_Deserialize_ExtensionOutput) + [_Info # 1];
    serverNamespace setVariable ["DiscordEmbedBuilder_Info", _Webhook];
    missionNamespace setVariable ["DiscordEmbedBuilder_Info", _Webhook,true];

    0 spawn {
      waitUntil { !isNull findDisplay 46 };

      //- Fire postInit Event
      INFO(MSG_INIT);
      [QGVARMAIN(postInit_Server)] call CBA_fnc_LocalEvent;

      //- Check Mission Ended (on Server Shutdown)
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
