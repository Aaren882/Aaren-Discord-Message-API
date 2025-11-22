class CfgPatches
{
	class DiscordMessageAPI
	{
		units[] = {};
		weapons[] = {};
		requiredVersion = 2.00;
		requiredAddons[] = 
		{
			"A3_Data_F"
		};
	};
};

class Extended_PreInit_EventHandlers 
{
	class DiscordMessageAPI_EH
	{
		init = "call compile preprocessFileLineNumbers 'z\DiscordAPI\addons\main\XEH_preInit.sqf'";
	};
};
class Extended_PostInit_EventHandlers 
{
	class DiscordMessageAPI_EH
	{
		init = "call compile preprocessFileLineNumbers 'z\DiscordAPI\addons\main\XEH_postInit.sqf'";
	};
};

class CfgFunctions
{
	class DiscordAPI
	{
		class init
		{
			file="\z\DiscordAPI\addons\main\functions\init";
			class init_player;
			class refresh_webhooks;
		};
		class functions
		{
			file="\z\DiscordAPI\addons\main\functions";
			class sendMessage;
			class sendJson;
			class sendJsonFormat;
			class ServerInfo_Loop;

			class Update_ServerInfo;
		};
	};
};