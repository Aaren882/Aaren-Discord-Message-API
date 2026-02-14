#include "script_component.hpp"

//- Check Current Webhooks Count
private _infoVar = missionNamespace getVariable ["DiscordEmbedBuilder_Info",[]];

TRACE_1("fn_refresh_webhooks",_infoVar);
if (_infoVar findIf {true} < 0) exitWith {};

#define LOCAL_STR(STRING) localize ("STR_Discord_MSG_" + STRING)

//- Setup CBA setting
  [
    hashValue "DiscordMessageAPI_ServerID", "EDITBOX", 
    [
      LOCAL_STR("Edit_ServerID"),
      LOCAL_STR("Edit_ServerID_Tip")
    ], 
    ["DiscordMessageAPI Settings", LOCAL_STR("Server_INFO")], 
    "",
    1,
    {
      if !(isServer) exitWith {};
      serverNamespace setVariable ["DiscordMessageAPI_ServerID", _this];
    }
  ] call CBA_fnc_addSetting;

//- Server Infos
  [
    hashValue "DiscordMessageAPI_ServerJSON", "EDITBOX", 
    [
      LOCAL_STR("SERVERINFO_JSON"),
      LOCAL_STR("SERVERINFO_JSON_Tip")
    ], 
    ["DiscordMessageAPI Settings", LOCAL_STR("Server_INFO")], 
    "Server_Info_msg.json",
    1,
    {
      if !(isServer) exitWith {};
      serverNamespace setVariable ["DiscordMessageAPI_ServerJSON", _this];
    }
  ] call CBA_fnc_addSetting;

//- Shutdown Msg
  [
    hashValue "DiscordMessageAPI_ClosedJSON", "EDITBOX", 
    [
      LOCAL_STR("Closed_JSON"),
      LOCAL_STR("Closed_JSON_Tip")
    ], 
    ["DiscordMessageAPI Settings", LOCAL_STR("Server_INFO")], 
    "Server_Ended_msg.json",
    1,
    {
      if !(isServer) exitWith {};
      serverNamespace setVariable ["DiscordMessageAPI_ClosedJSON", _this];
    }
  ] call CBA_fnc_addSetting;

/////////////////////////////////////////////////////////////////////////////////
//- Refresh Webhook select List
  private _list = [];
  for "_i" from 0 to (count (_infoVar # 0)) - 1 do {
    _list pushBack _i;
  };

  private _setup = {
    params ["_CONST", "_Title"];

    private _fnc = switch (_CONST) do {
      case "DiscordMessageAPI_WebhookSel": {
        {
          if !(isServer) exitWith {};
          serverNamespace setVariable ["DiscordMessageAPI_WebhookSel", _this];
        }
      };

      case "DiscordMessageAPI_ServerWebhookSel": {
        {
          if !(isServer) exitWith {};
          serverNamespace setVariable ["DiscordMessageAPI_ServerWebhookSel", _this];
        }
      };
    };

    //- Output
    [
      _CONST, "LIST", 
      [
        _Title,
        LOCAL_STR("Webhook_Sel_Tip")
      ],
      ["DiscordMessageAPI Settings",LOCAL_STR("Webhook")], 
      [
        _list,
        _list apply {str _x},
        0
      ],
      1,
      _fnc //- Refresh Sel Webhook
    ]
  };


  //- Apply Settings
  {
    (_x call _setup) call CBA_fnc_addSetting;
  } forEach [
    ["DiscordMessageAPI_WebhookSel",LOCAL_STR("Webhook_Sel")],
    ["DiscordMessageAPI_ServerWebhookSel",LOCAL_STR("SERVER_INFO_Webhook_Sel")]
  ];

true
