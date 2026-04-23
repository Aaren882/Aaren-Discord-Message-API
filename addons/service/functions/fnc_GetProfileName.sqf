#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_GetProfileName
Description:
    Retrieves the profile name used for the server connection identity.
    This name is used to link the server instance with the backend service.

Parameters:
    None

Returns:
    _profileName - The stored server profile name or an empty string if not set <STRING>

Examples
    (begin example)
        call DiscordAPI_service_fnc_GetProfileName
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

TRACE_1("fnc_GetProfileName",_this);

localNamespace getVariable [QGVAR(serverName), ""];
