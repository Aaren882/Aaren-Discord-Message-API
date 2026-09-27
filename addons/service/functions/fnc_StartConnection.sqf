#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_StartConnection
Description:
    Initiates the connection to the backend service by sending the necessary profile information and message content.
    This function is typically called during the mission startup sequence to establish communication with the backend.

Parameters:
    <NONE>

Returns:
    <NONE>

Examples
    (begin example)
        call DiscordAPI_service_fnc_StartConnection
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

private _params = [call FUNC(GetProfileName), GVAR(Profiles)];
INFO_1("Try to Connect : %1",_params);

"DiscordMessageAPIService" callExtension ["ConnectWebSocket", _params];
