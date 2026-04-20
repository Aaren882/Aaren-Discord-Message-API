#include "script_component.hpp"

//- Extension callBack tunnel
[QGVARMAIN(postInit_Server), {

  INFO("DISCORD_API [CallBack Init]");
  addMissionEventHandler ["ExtensionCallback", 
  {
    params ["_name", "_callBackType", "_data"];
    if (_name isNotEqualTo "DISCORD_API") exitWith {}; //- Check source

    TRACE_1("ExtensionCallback",_data);
    private _props = fromJSON _data;

    //- Specify callback Type
    switch (parseNumber _callBackType) do {
      case __Text__: {
        private _message = _props getOrDefault ["Message",""];
        
        INFO_1("DISCORD_API [CallBack Text] || Message : %1",_message);
      };
      
      case __Rpt__: {
        /*
        string FileName,
        long FileSize,
        DateTime CreatedTime,
        int TotalChunks
        */
        private _fileName = _props getOrDefault ["FileName",""];
        private _fileSize = _props getOrDefault ["FileSize",0];
        private _createdTime = _props getOrDefault ["CreatedTime",""];
        private _totalChunks = _props getOrDefault ["TotalChunks",0];
        INFO_4("DISCORD_API [CallBack Rpt] || _FileName : %1 , _FileSize : %2 , _CreatedTime : %3 , _TotalChunks : %4",_fileName,_fileSize,_createdTime,_totalChunks);
      };
      
      case __Command__: {
        private _eventName = _props getOrDefault ["Function",""];
        private _dta = _props getOrDefault ["Data", "[]"];

        private _event = QUOTE(ADDON) + "_" + _eventName;
        INFO_2("DISCORD_API [CallBack Command] || Event : %1 , Data : %2",_event,_dta);

        [_event, parseSimpleArray _dta] call CBA_fnc_localEvent;
      };

      //- Structured Data
      case __JsonString__: {
        private _jsonString = _props getOrDefault ["JsonString","{}"];
        INFO_1("DISCORD_API [CallBack JsonString] || JsonString : %1",_jsonString);
      };
      case __FlatJsonString__: {
        private _flatJsonString = _props getOrDefault ["FlatJsonString",[]];
        INFO_1("DISCORD_API [CallBack FlatJsonString] || FlatJsonString : %1",_flatJsonString);
      };
      default {
        ERROR_1("Invalid callback type ""%1""",_callBackType);
      };
    };
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
