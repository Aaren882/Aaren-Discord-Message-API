#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: Discord_service_fnc_GetPathFiles
Description:
    Retrieves a list of file names from a specified directory path relative to the server root.
    The function calls an extension to access the file system and returns an array of file names found in the specified directory.

Parameters:
    _path - The directory path relative to the server root to scan for files <STRING>

Returns:
    _fileList - An array of file names found in the specified directory <ARRAY>

Returns:
 <NONE>

Examples
    (begin example)
        ["profiles"] call Discord_service_fnc_GetPathFiles
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params [
  ["_path","",[""]]
];

private _paths = "DiscordMessageAPIService" callExtension ["GetDirectoryFileNames",[_path]];
TRACE_1("fnc_GetPathFiles",_paths # 0);

// Return
parseSimpleArray (_paths # 0);
