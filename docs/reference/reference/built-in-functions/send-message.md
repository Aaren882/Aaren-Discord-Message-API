# 📨 Send Message

## SQF script Syntax:

```sqf
[
    _webhook_Sel, //- Index of Webhook (0,1,2 etc)
    _content, //- Main message
    _user, //- User Name
    _avatar, //- Picture Link
    _tts, //- Text to Speech
    _file, //- <String> File-Directory 
    _embeds, //- Look down 👇
    _fields //- Fields for Embeds 👇
] call DiscordAPI_fnc_sendMessage;
```

{% hint style="info" %}
_**\_embeds**_ Format : (will overwrite _**\_content**_)

```
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

/////- Example -/////
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
```
{% endhint %}

***

## Quick Example 🐊

```sqf
[
    0,
    "CONTENT GET OVERWRITEN",
    "Example USER",
    "https://i.imgur.com/410CmKi.png",
    false,
    "mod.cpp",
    [
      [
        "Sent From Client",
        "ABABA"
      ]
    ],
    [
      [
        ["W","E",true],
        ["2","VAL",true],
        ["","",false],
        ["3","VAL",true],
        ["4","VAL",true]
      ]
    ]
] call DiscordAPI_fnc_sendMessage;
```

### Another Example :horse:

```sqf
[
    0,
    "IM **CONTENT** *MESSAGE*",
    "Example USER",
    "https://i.imgur.com/410CmKi.png"
] call DiscordAPI_fnc_sendMessage;
```

***
