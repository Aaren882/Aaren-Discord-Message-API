#include "script_component.hpp"
/* ----------------------------------------------------------------------------
Function: DiscordAPI_fnc_sendJsonFormat
Description:
    Sends a formatted JSON message to Discord using a webhook.
    The function takes a JSON string (or file content) and a payload containing 
    webhook information and message handling instructions.

Parameters:
    _msg      - JSON string or file content to be sent <STRING>
    _Sel      - Webhook index <NUMBER>
    _payload  - Additional payload data like HandlerType and MessageID <ARRAY>

Returns:
    Extension return <ARRAY>

Examples
    (begin example)
        [
          "{ ""JSON_KEY"" : ""JSON_VALUE"" }",
          0,
          [
            ["HandlerType", 0],
            ["MessageID", ""]
          ]
        ] call DiscordAPI_fnc_sendJsonFormat
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */

params [
  "_msg",
  ["_Sel", DiscordMessageAPI_WebhookSel],
  "_payload"
];

TRACE_2("fnc_sendJsonFormat",_Sel,_payload);

if (isNil "_Sel" || isNil "_payload") exitWith {
  ERROR_3("""fnc_sendJsonFormat"" Exception : ""_Sel"" = %1, ""_msg"" = %2, ""_payload"" = %3",_Sel,_msg,_payload);
};

private _isV2 = "v2" in GVAR(ExtensionVersion);
private _url = DiscordEmbedBuilder_Info # 0 # _Sel;

//- Struct hashMap
private _headerMap = createHashMapFromArray _payload;
TRACE_1("fnc_sendJsonFormat (PAYLOAD HEADER)",_headerMap);

private _argHeader = if (_isV2) then {
  _headerMap set ["Url", _url];

  toJSON _headerMap
} else {

  private _type = _headerMap getOrDefault ["HandlerType", 0];
  private _messageId = _headerMap getOrDefault ["MessageID", ""];
  
  private _result = switch (_type) do {
    case 1: { //- Refresh
      if (_messageId isEqualTo "") then {
        throw "Invalid _messageId. please make sure ""_payload"" has valid ""MessageID"" property.";
      };
      [_url, _type, _messageId]
    };
    default {
      [_url, _type]
    };
  };

  _result
};

//- Send Format Json
[ 
  "HandlerJsonFormat", 
  [
    _argHeader,
    _msg
  ] 
] call FUNC(callExtension);
