class CfgExtensions {
  class ADDON {
    extensions[] = {"discordmessageapiservice"}; // Whitelist, which extension names to run this check on (so its not ran on unrelated extensions we don't care about)
    onCheckLoad = QUOTE(call COMPILE_FILE_SYS(PREFIX,main,functions\fn_checkExtension));

    // The handler has access to the config class, so we can store some properties here)
    #include "\z\publicKey.hpp"
  };
};
