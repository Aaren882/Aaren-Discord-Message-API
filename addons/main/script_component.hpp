#define COMPONENT main

#include "script_mod.hpp"

#define DEBUG_ENABLED_MAIN
// #define DEBUG_MODE_FULL
// #define DISABLE_COMPILE_CACHE

#ifdef DEBUG_ENABLED_MAIN
    #define DEBUG_MODE_FULL
#endif
#ifdef DEBUG_SETTINGS_MAIN
    #define DEBUG_SETTINGS DEBUG_SETTINGS_MAIN
#endif

#define LOCAL_STR(STRING) localize ("STR_Discord_MSG_" + STRING)
#include "script_macros.hpp"
