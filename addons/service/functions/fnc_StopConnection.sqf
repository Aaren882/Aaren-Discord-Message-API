#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_service_fnc_StopConnection
Description:
    Terminates the connection to the backend service by sending a disconnect command to the extension.
    This ensures that the WebSocket connection is properly closed when the server shuts down or the mission ends.

Parameters:
    None

Returns:
    None

Examples:
    (begin example)
        call DiscordAPI_service_fnc_StopConnection
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

INFO("Closing WebSocket connection");
"DiscordMessageAPI" callExtension ["DisconnectWebSocket", []];
