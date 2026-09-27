#include "script_component.hpp"
/*
Function: DiscordAPI_service_fnc_AdminPanel
Description:
    Manages the admin panel controls for players with appropriate permissions. This function is triggered by specific events (e.g., player connection, login, logout) 
    adds/removes diary controls for service management based on the player's state and permissions.

Parameters:
    _PID - The player ID of the target player for whom the admin panel controls should be managed <STRING>
    _event - The event type that triggered the function, expected values are "login" or "logout" <STRING>

Returns:
    None

Examples:
    (begin example)
        [getPlayerID player, "login"] call DiscordAPI_service_fnc_AdminPanel
    (end)

Author:
    Aaren
*/

params ["_PID",	["_event", "", [""]]];

if (isNil {_PID}) exitWith {
  ERROR_1("Admin Panel: called with nil PID, event: %1",_event);
};

private _userInfo = getUserInfo _PID;
if (isNil {_userInfo}) exitWith {
  ERROR_1("Admin Panel: getUserInfo returned nil for PID: %1",_PID);
};
if (
  !(_userInfo isEqualType []) || 
  count _userInfo < 11
) exitWith {
  private _typeName = typeName _userInfo;
  private _userInfoCount = [-1, count _userInfo] select (_userInfo isEqualType []);
  ERROR_4("Admin Panel: getUserInfo returned unexpected data for PID %1: type=%2 count=%3 data=%4",_PID,_typeName,_userInfoCount,_userInfo);
};

_userInfo params ["", "_ownerId", "_playerUID"];
private _unit = _userInfo # 10;
if (isNull _unit) exitWith {
  ERROR_2("Admin Panel: getUserInfo unit (index 10) is null for PID %1 (UID: %2)",_PID,_playerUID);
};

private _fnc_addControls = {
	params ["_ownerId", "_unit"];

  //- Config Infos
  private _configuration = [
    ["Server", call FUNC(GetProfileName)],
    ["RPT", call FUNC(GetCurrentRptFilePath)]
  ] apply {
    _x params ["_title", "_content"];
    format ["* %1: <font color='#888888'>""%2""</font>", _title, _content];
  };

  //- Add diary controls to admin (client)
	[
		{ getClientStateNumber > 9 && !isNull player },
    {
      params ["_configuration"];
      
      player createDiarySubject [
				QGVAR(adminPanel_diary),
				"Discord Admin",
				"\A3\ui_f\data\igui\cfg\simpleTasks\types\interact_ca.paa"
			];

      //- Config Infos
      private _diaryControls = [
        ["Start Connection", QGVAR(StartConnection)],
        ["Stop Connection", QGVAR(StopConnection)]
      ] apply {
        _x params ["_title", "_triggerEvent"];
        format ["<execute expression='[""%1""] call CBA_fnc_serverEvent;'>%2</execute>", _triggerEvent, _title]
      };

			GVAR(adminPanel) = player createDiaryRecord [
				QGVAR(adminPanel_diary),
				[
					"Controls",
					format[
						"<font color='#FFD700' size='16'>Service Management</font><br/>%1<br/><br/>%2<br/><br/>%3",
            "The service profile can be changed in the <font color='#7289da'>CBA Addon Options</font> (Server tab).",
            _configuration joinString "<br/>",
						_diaryControls joinString "<br/>"
					]
				]
			];
		},
    [_configuration],
    5,
    {
      //- Log on Client
      WARNING("Admin Panel: Client state wait timeout. Diary entry not created.");
    }
	] remoteExec ["CBA_fnc_waitUntilAndExecute", _ownerId];

	  // set variable on unit
	_unit setVariable [QGVAR(hasAdminPanel), true];
};

private _fnc_removeControls = {
	params ["_ownerId", "_unit"];
	{
		player removeDiarySubject QGVAR(adminPanel_diary);
		player setVariable [QGVAR(hasAdminPanel), false, 2];
	} remoteExec ["call", _ownerId];

  //- Save on Server-side missionNamespace
	_unit setVariable [QGVAR(hasAdminPanel), false];
};

private _hasAdminPanel = _unit getVariable [QGVAR(hasAdminPanel), false];
switch (_event) do {
	case "login": {
		if !(_hasAdminPanel) then {
			[_ownerId, _unit] call _fnc_addControls;
		};
	};
	case "logout": {
		if (_hasAdminPanel) then {
			[_ownerId, _unit] call _fnc_removeControls;
		};
	};
	default {};
};
