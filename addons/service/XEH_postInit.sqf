#include "script_component.hpp"

//- Extension callBack tunnel
[QGVARMAIN(postInit_Server), {

  INFO("DISCORD_API [CallBack Init]");
  addMissionEventHandler ["ExtensionCallback", 
  {
    params ["_name", "_eventName", "_data"];
    if (_name isNotEqualTo "DISCORD_API") exitWith {}; //- Check source

    private _event = QUOTE(ADDON)+ "_" + _eventName;
    INFO_2("DISCORD_API [CallBack] || Event : %1 , Data : %2 ",_event,_data);
  
    [_event, parseSimpleArray _data] call CBA_fnc_localEvent;
  }];

  INFO("DISCORD_API [Server Info Init]");
  call FUNC(ServerInfo_Loop);
  
  //- Check Server Entry & Exit
  addMissionEventHandler ["PlayerConnected", {
    [true] call FUNC(UpdateWebhook_ServerInfo);
  }];
  addMissionEventHandler ["HandleDisconnect", {
    [true] call FUNC(UpdateWebhook_ServerInfo);
  }];
}] call CBA_fnc_addEventHandler;

[QGVAR(ConnectionChanged), FUNC(SetServiceAvailability)] call CBA_fnc_addEventHandler;
