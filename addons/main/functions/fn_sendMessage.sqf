#include "script_component.hpp"

/* ----------------------------------------------------------------------------
Function: DiscordAPI_fnc_sendMessage
Description:
    Send a custom message to Discord using a webhook.

Parameters:
    _webhook_Sel - Webhook index <NUMBER>
    _content     - Message content <STRING>
    _user        - Override username <STRING>
    _avatar      - Override avatar URL <STRING>
    _tts         - Text-to-speech <BOOL>
    _file        - Path to file attachment <STRING>
    _embeds      - Array of embed configurations <ARRAY>
    _fields      - Array of fields for each embed <ARRAY>

Returns:
    Extension return <ARRAY>

Examples
    (begin example)
        [
          0,
          "Hello Discord!",
          "Arma 3 Server",
          "",
          false,
          "",
          [["Title", "Description"]],
          [[["Field1", "Value1", true]]]
        ] call DiscordAPI_fnc_sendMessage
    (end)

Author:
    Aaren
---------------------------------------------------------------------------- */
if (isNil{DiscordEmbedBuilder_Info}) exitWith {};

params [
  ["_webhook_Sel", DiscordMessageAPI_WebhookSel],
  ["_content",""],
  ["_user",""],
  ["_avatar",""],
  ["_tts",false],
  ["_file",""],
  ["_embeds",[]],
  ["_fields",[]]
];
TRACE_1("fn_sendMessage",_this);

private _payload = createHashMapFromArray [
  ["Url", DiscordEmbedBuilder_Info # 0 # _webhook_Sel],
  ["HandlerType", 0],
  ["MessageID", ""]
];

"DiscordMessageAPI" callExtension [
  "SendMessage",
  [
    toJSON _payload, //- [Webhook, Mode]
    _content, //- Content
    _user, //- User name
    _avatar, //- Avatar
    _tts, //- TTS
    [str (toArray _file),""] select (_file == ""), //- File
    _embeds,
    _fields
  ]
];

/**********Embeds************
  Title -                                        "Title"
  Description -                                  "DESC"
  Color -                                        "000000"
  timestamp -                                    true
  AuthorName -                                   profileName
  AuthorUrl -                                    "https://steamcommunity.com/id/_connor"
  AuthorIconUrl -                                "https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avatars/1e/1e3c83b65d6f34cc9708eae853e8bc9848865dd1_full.jpg"
  ImageUrl ["Http(s)://" OR "attachment://"] -   "https://arma3.com/assets/img/wallpapers/low/3/Arma%203%20Laws%20of%20War_wallpaper_1024x768.jpg"
  ThumbnailUrl -                                 "https://arma3.com/assets/img/wallpapers/1/9/arma3_white_plain_800x600.jpg"
  FooterText -                                   "This is the footer text"
  FooterIconUrl -                                "https://steamcdn-a.opskins.media/steamcommunity/public/images/apps/107410/3212af52faf994c558bd622cb0f360c1ef295a6b.jpg"

  //- Example
    [ //- Embeds
      [
        "Sent From Client",
        "ABABA"
      ]
    ],
    [ //- Fields for each Embed
      [
        ["W","E",true],
        ["2","VAL",true],
        ["","",false],
        ["3","VAL",true],
        ["4","VAL",true]
      ]
    ]
***************************/
