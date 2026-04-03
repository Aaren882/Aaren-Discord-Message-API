#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_getServerInfo
Description:
    Get current server status and player information formatted for Discord.
    This function gathers mission details, player counts, system time, and performance metrics.

Parameters:
    None

Returns:
    _infoList - Array of [Key, Value] pairs for JSON replacement <ARRAY>

Examples
    (begin example)
        call DiscordAPI_service_fnc_getServerInfo
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

TRACE_1("fnc_GetServerInfo",_this);

//- Get User List
private _list = allUsers select {
  private _info = getUserInfo _x;
  !(_info # 7) && ([(11 > _info # 6), true] select DiscordMsg_API_isPersistent)
};

//- Exit with Shutdown Msg
#ifndef DEBUG_MODE_FULL
  if (_list isEqualTo [] && !DiscordMsg_API_isPersistent) exitWith {_list};
#endif

//- Count Headless Clients
  private _headless = {(getUserInfo _x) # 7} count allUsers;

_list = _list apply {
  private _info = getUserInfo _x;

  private _Connection_state = _info # 6;

  //- Output
  [
    format ["%1 \n ", _info # 4], //- Name [Squad Info]
    format ["”%1” \n ", localize ("STR_Discord_MSG_INFO_" + ([str _Connection_state,"2"] select (_Connection_state > 10)))], //- Connection State
    format ["%1 \n ", _info # 9] //- NetWork Info [ping, bandwidth, desync]
  ]
};


//- Count Slots
private _slots = 0;
private _eachSlot = [blufor, opfor, independent, civilian] apply {
  private _i = playableSlotsNumber _x;
  _slots = _slots + _i;
  _i
};


//- Get current systemTime
private _SysDate= [];
private _SysTime= [];
private _systemTime = systemTime;

for "_i" from 0 to 2 do {
  private _date = _systemTime # _i;

  _SysDate pushBack ((["", "0"] select (_date < 10)) + str _date);

  private _t = _systemTime # (3 + _i);
  _SysTime pushBack ((["", "0"] select (_t < 10)) + str _t);
};

//- Result
[
  ["MAP_NAME", worldName],
  ["SERVER_NAME", serverName],
  ["MISSION_NAME", briefingName],
  
  ["PLAYER_COUNT", str (count allPlayers)],
  ["BLUFOR_COUNT", str (playersNumber blufor)],
  ["OPFOR_COUNT", str (playersNumber opfor)],
  ["INDEP_COUNT", str (playersNumber independent)],
  ["CIV_COUNT", str (playersNumber civilian)],

  ["AVALIABLE_PLAYERS", str _slots],
  ["AVALIABLE_BLUFOR", str (_eachSlot # 0)],
  ["AVALIABLE_OPFOR", str (_eachSlot # 1)],
  ["AVALIABLE_INDEP", str (_eachSlot # 2)],
  ["AVALIABLE_CIV", str (_eachSlot # 3)],

  ["GAME_VERSION", format ["%1.%2", (productVersion # 2) * 0.01 toFixed 2, productVersion # 3]],
  ["SYSTEM_DATE", _SysDate joinString "-"],
  ["SYSTEM_TIME", _SysTime joinString ":"],
  ["SERVER_FPS", diag_fps toFixed 2],
  ["FPS_MIN", diag_fpsMin toFixed 2],
  ["ACTIVE_SCRIPTS", str diag_activeScripts],

  ["HEADLESS", str _headless],
  ["PLAYER_LIST", (_list apply {_x # 0}) joinString ""],
  ["PLAYER_STATE", (_list apply {_x # 1}) joinString ""],
  ["PLAYER_NETWORK", (_list apply {_x # 2}) joinString ""]
]
