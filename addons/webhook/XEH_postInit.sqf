#include "script_component.hpp"

INFO(MSG_INIT);

//- Must Be Multiplayer
#ifndef DEBUG_MODE_FULL
  if !(isMultiplayer) exitWith {};
#endif

0 spawn {
  private _TimeOuted = {
    params ["_startTime"];
    (diag_tickTime - _startTime) >= 5
  };

  private _start = diag_tickTime;
  if (isServer) then {
    try {
      waitUntil {
        !isNil{GVAR(ExtensionVersion)} ||
        _start call _TimeOuted
      };

      if (isNil{GVAR(ExtensionVersion)})
        throw format ["Initialization Timed out! ""%1"" Addon option still nil.", QGVAR(ExtensionVersion)];

      //- Return webhooks counts (#NOTE : Backward compat)
        private _IsV2 = "v2" in GVAR(ExtensionVersion);
        private _Info = if (_IsV2) then {
          ["Init_Server",[]] call FUNC(callExtension);
        } else {
          ["Refresh_Webhooks",[-1]] call FUNC(callExtension);
        };

        INFO_1("Initializing Server (IsV2=%1).",_IsV2);
        if (isNil "_Info") throw "Extension failed to return necessary webhook initialization data.";

        private _Webhook = ((_Info # 0) call DiscordAPI_fnc_Deserialize_ExtensionOutput) + [_Info # 1];
        serverNamespace setVariable ["DiscordEmbedBuilder_Info", _Webhook];
        missionNamespace setVariable ["DiscordEmbedBuilder_Info", _Webhook, true];

      INFO_1("Initialized. Webhook ""%1"" scanned.",count _Webhook);

      [QGVARMAIN(ServerInfoLoop), FUNC(Update_ServerInfo)] call CBA_fnc_addEventHandler;
      INFO("Server Monitoring service successfully registered.");

      //- Unload EventHandler
      [QGVARMAIN(Mission_Unload_Server), {
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
        ] call FUNC(sendJsonFormat);
      }] call CBA_fnc_addEventHandler;
    } catch {
      ERROR_1("Failed to initiate Webhooks. Please make sure ""webhooks.json"" and other templates is exist at correct directory. Exception: ""%1"".",_exception);

      [{
        ["[DISCORD API | WEBHOOK] Failed to initiate Webhooks on Server. Check mission RPT for more details."] remoteExecCall ["systemChat", 0];
      }] call CBA_fnc_execNextFrame;
    };
    
  } else {
    //- Init Clients
    try {
      waitUntil {
        !isNil{DiscordEmbedBuilder_Info} ||
        _start call _TimeOuted
      };
      if (isNil{DiscordEmbedBuilder_Info})
        throw "Initialization Timed out! Server didn't initialize in time.";

      call FUNC(init_player);
      [QGVARMAIN(postInit_Client)] call CBA_fnc_LocalEvent;
    } catch {
      ERROR_1("Failed to initiate Client Webhooks. Exception: %1",_exception);

      systemChat str "[DISCORD API | WEBHOOK] Failed to initiate Webhooks on Client. Check mission RPT for more details.";
    };
  };

  //- initiate for Server and Client (for CBA Settings)
  call FUNC(refresh_webhooks);
};
