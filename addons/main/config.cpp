#include "script_component.hpp"

class CfgPatches
{
	class DiscordMessageAPI
	{
		authors[] = {"Aaren"};
		url = ECSTRING(main,url);
		requiredVersion = REQUIRED_VERSION;
		requiredAddons[] = {"A3_Data_F"};
		units[] = {};
		weapons[] = {};
		VERSION_CONFIG;
	};
  class ADDON: DiscordMessageAPI {};
};

class Extended_PreInit_EventHandlers 
{
	class ADDON
	{
		init = QUOTE(call COMPILE_FILE(XEH_PreInit));
	};
};
class Extended_PostInit_EventHandlers 
{
	class ADDON
	{
		init = QUOTE(call COMPILE_FILE(XEH_postInit));
	};
};

class CfgFunctions
{
	class DiscordAPI
	{
		class init
		{
			file=QPATHTOF(functions\init);
			class init_player {};
			class refresh_webhooks {};
		};
		class functions
		{
			file=QPATHTOF(functions);
			class FormatJson {};
			class GetPathFiles {};
			class Deserialize_ExtensionOutput {};
		};
    //#TODO - Deprecate these
		class Deprecation
		{
			class sendMessage
      {
        file=QPATHTOEF(webhook,functions\fnc_sendMessage.sqf);
      };
		};
	};
};

#include "CfgSettings.hpp"
