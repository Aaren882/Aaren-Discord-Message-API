#include "script_component.hpp"

INFO(MSG_INIT);

[QGVARMAIN(postInit_Server), { //- Extension callBack tunnel

  INFO("DISCORD_API [CallBack Init]");
  addMissionEventHandler ["ExtensionCallback", 
  {
    params ["_name", "_function", "_data"];
    if (_name isNotEqualTo "DISCORD_API") exitWith {}; //- Check source

    INFO_3("DISCORD_API [CallBack]",_name,_function,_data);

    private _fnc = uiNamespace getVariable [_function, {
      systemChat "No callBack function is found.";
      ERROR("No callBack function is found.");
    }];

    _data call _fnc;
  }];
}] call CBA_fnc_addEventHandler;
