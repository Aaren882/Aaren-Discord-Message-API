// Function Name: ["_file","_formats","_Sel"] call DiscordAPI_fnc_sendJsonFormat

params ["_file","_formats",["_Sel",DiscordMessageAPI_WebhookSel],["_payload",[0]]];

if (isNil{_Sel} || _file == "") exitWith {};

/* 
[
  //- both "Key" and "Value" are Strings
  
  ["Key", "Value"],
  ["{SERVER_IP}", "192.168.xx.xx"],
  ["{PLAYER_COUNT}", str (count allUsers)],
  ["Key", "Value"]
] 

//- Example of Sending JSON Payload
  [
    "Server_Info_msg.json",
    [],
    DiscordMessageAPI_ServerWebhookSel
  ] call DiscordAPI_fnc_sendJsonFormat;
*/

private _msg = toString parseSimpleArray (("DiscordMessageAPI" callExtension [
  "ParseJson", 
  [//- File Directory
    _file
  ] 
]) # 0);

{
  _msg = [_msg,_x # 0,_x # 1] call CBA_fnc_replace;
} forEach _formats;

//- Send Format Json

"DiscordMessageAPI" callExtension [ 
  "HandlerJsonFormat", 
  [
    [DiscordEmbedBuilder_Info # 0 # _Sel] + _payload,
    _msg
  ] 
];