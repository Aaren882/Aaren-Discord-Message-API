---
description: List of Keys explains
---

# 🪄 Customize Server-INFO

{% hint style="info" %}
**Good to know:**

You can choose desire _**json**_ file for current Server Profile.

_(same as the setting for <mark style="color:orange;">**Mission Closed**</mark>)_

<img src="../.gitbook/assets/image (3).png" alt="" data-size="original">
{% endhint %}

<table data-full-width="false"><thead><tr><th width="249" align="center">Keys</th><th align="center">Description</th></tr></thead><tbody><tr><td align="center">{MAP_NAME}</td><td align="center">Check <a href="https://community.bistudio.com/wiki/worldName?useskin=darkvector"><em><strong>worldName</strong></em></a> Command</td></tr><tr><td align="center">{SERVER_NAME}</td><td align="center">Check <a href="https://community.bistudio.com/wiki/serverName?useskin=darkvector"><em><strong>serverName</strong></em></a> Command</td></tr><tr><td align="center">{MISSION_NAME}</td><td align="center">Check <a href="https://community.bistudio.com/wiki/briefingName?useskin=darkvector"><em><strong>briefingName</strong></em></a> Command</td></tr><tr><td align="center">{PLAYER_COUNT}</td><td align="center">Simply current player count</td></tr><tr><td align="center">{BLUFOR_COUNT}</td><td align="center">Current <strong>BLUFOR</strong> player count</td></tr><tr><td align="center">{OPFOR_COUNT}</td><td align="center">Current <strong>OPFOR</strong> player count</td></tr><tr><td align="center">{INDEP_COUNT}</td><td align="center">Current <strong>INDEPENDENT</strong> player count</td></tr><tr><td align="center">{CIV_COUNT}</td><td align="center">Current <strong>CIVILIAN</strong> player count</td></tr><tr><td align="center">{AVALIABLE_PLAYERS}</td><td align="center">All player slots</td></tr><tr><td align="center">{AVALIABLE_BLUFOR}</td><td align="center">All player slots <strong>BLUFOR</strong></td></tr><tr><td align="center">{AVALIABLE_OPFOR}</td><td align="center">All player slots <strong>OPFOR</strong></td></tr><tr><td align="center">{AVALIABLE_INDEP}</td><td align="center">All player slots <strong>INDEPENDENT</strong></td></tr><tr><td align="center">{AVALIABLE_CIV}</td><td align="center">All player slots <strong>CIVILIAN</strong></td></tr><tr><td align="center">{GAME_VERSION}</td><td align="center">Check <a href="https://community.bistudio.com/wiki/productVersion?useskin=darkvector"><em><strong>productVersion</strong></em></a> Command (<em>Example 3</em>)</td></tr><tr><td align="center">{SYSTEM_DATE}</td><td align="center">Date from (<a href="https://community.bistudio.com/wiki/systemTime?useskin=darkvector"><em><strong>systemTime</strong></em></a> Command)</td></tr><tr><td align="center">{SYSTEM_TIME}</td><td align="center">Same as above, but Time</td></tr><tr><td align="center">{SERVER_FPS}</td><td align="center">Current Server FPS (<a href="https://community.bistudio.com/wiki/diag_fps?useskin=darkvector"><em><strong>diag fps</strong></em></a> Command)</td></tr><tr><td align="center">{FPS_MIN}</td><td align="center">Current Server minimal FPS (<a href="https://community.bistudio.com/wiki/diag_fpsMin?useskin=darkvector"><em><strong>diag fpsMin</strong></em></a> Command)</td></tr><tr><td align="center">{ACTIVE_SCRIPTS}</td><td align="center">Check <a href="https://community.bistudio.com/wiki/diag_activeScripts?useskin=darkvector"><em><strong>diag activeScripts</strong></em></a> Command</td></tr><tr><td align="center">{HEADLESS}</td><td align="center">Current <em><strong>Headless Clients</strong></em> Count</td></tr><tr><td align="center">{PLAYER_LIST}</td><td align="center">Player name including squad (see <a href="https://community.bistudio.com/wiki/name?useskin=darkvector"><em><strong>name</strong></em></a>, <a href="https://community.bistudio.com/wiki/squadParams?useskin=darkvector"><em><strong>squadParams</strong></em></a>)</td></tr><tr><td align="center">{PLAYER_STATE}</td><td align="center">Current player State (<a href="https://community.bistudio.com/wiki/getClientStateNumber?useskin=darkvector"><em><strong>getClientStateNumber</strong></em></a> Command)</td></tr><tr><td align="center">{PLAYER_NETWORK}</td><td align="center">Format <em><strong>[ping, bandwidth, desync]</strong></em></td></tr></tbody></table>

{% hint style="danger" %}
Make sure "<mark style="color:yellow;">{ }</mark>" are included.
{% endhint %}

***

## Lost the Message File ?

{% file src="../.gitbook/assets/Server_Info_msg.json" %}
Original File
{% endfile %}
