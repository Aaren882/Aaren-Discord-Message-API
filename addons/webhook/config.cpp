#include "script_component.hpp"

class CfgPatches
{
	class ADDON
	{
		authors[] = {"Aaren"};
		url = ECSTRING(main,url);
		requiredVersion = REQUIRED_VERSION;
		requiredAddons[] = {QUOTE(MAIN_ADDON)};
		units[] = {};
		weapons[] = {};
		VERSION_CONFIG;
	};
};

class Extended_PreInit_EventHandlers 
{
	class ADDON
	{
		Init = QUOTE(call COMPILE_FILE(XEH_preInit));
	};
};
