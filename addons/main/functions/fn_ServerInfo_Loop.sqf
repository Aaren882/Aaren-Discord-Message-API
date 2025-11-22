[
  {
    [] call DiscordAPI_fnc_Update_ServerInfo;
    call DiscordAPI_fnc_ServerInfo_Loop;
  }, 
  [],
  DiscordMsg_API_Delay
] call CBA_fnc_waitAndExecute;