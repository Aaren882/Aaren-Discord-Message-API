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
};

class Extended_PreInit_EventHandlers 
{
	class DiscordMessageAPI_EH
	{
		init = QUOTE(call COMPILE_FILE(PreInit));
	};
};
class Extended_PostInit_EventHandlers 
{
	class DiscordMessageAPI_EH
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
			class init_player;
			class refresh_webhooks;
		};
		class functions
		{
			file=QPATHTOF(functions);
			class sendMessage;
			class sendJson;
			class sendJsonFormat;
			class ServerInfo_Loop;

			class Update_ServerInfo;
			class Deserialize_ExtensionOutput;
		};
	};
};
