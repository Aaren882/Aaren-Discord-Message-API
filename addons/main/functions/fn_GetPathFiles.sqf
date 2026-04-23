#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: Discord_fnc_GetPathFiles
Description:
    Retrieves a list of file names within a specific directory using the DiscordMessageAPI extension.
    This is primarily used to fetch available profile configurations from the 'profiles' folder.

Parameters:
    _path - The directory path relative to the server root to scan for files <STRING>

Returns:
    _fileList - An array of file names found in the specified directory <ARRAY>

Returns:
 <NONE>

Examples
    (begin example)
        ["profiles"] call Discord_fnc_GetPathFiles
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params [
  ["_path","",[""]]
];

TRACE_1("fn_GetPathFiles",_this);

_paths = "DiscordMessageAPI" callExtension ["GetDirectoryFileNames",[_path]];

//_paths
parseSimpleArray (_paths # 0);
