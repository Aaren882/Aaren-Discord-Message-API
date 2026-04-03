#include "script_component.hpp"

/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_SetServiceAvailability
Description:
    This function sets the global variable indicating whether the backend service is currently available.
    This state is typically updated via extension callbacks to ensure the server knows when it can successfully communicate with the service.

Parameters:
    _available  - <BOOL>

Returns:
    <NONE>

Author:
    Aaren
---------------------------------------------------------------------------- */
params ["_available"];

TRACE_1("fnc_SetServiceAvailability",_this);

private _diff = _available isNotEqualTo GVAR(Service_Available);

GVAR(Service_Available) = _available; //- (True/False)

if (_diff) then {
  [QGVAR(Service_Status_Changed), _available] call CBA_fnc_LocalEvent;
};
