#include "script_component.hpp"

private _metaDta = _this;

if !("signatures" in _metaDta) exitWith { true }; // This must be linux, it doesn't have signature reading. We'll just skip validation here

TRACE_1("fn_checkExtension",_metaDta);

//- Set variable for debug
#ifdef DEBUG_MODE_FULL
  uiNamespace setVariable [(_metaDta get "name"), _metaDta];
#endif

INFO_1("""fn_checkExtension"" Checking extension ""%1""",(_metaDta get "name"));

// must have signature, so signatures can't be empty
if ((_metaDta get "signatures") isEqualTo []) exitWith {
  ERROR("Extension is not signed.");
  false;
};

// Find a signature that is..
private _validSignatureFound = ((_metaDta get "signatures") apply {
  [
    (_x get "publicKey") == getText((_metaDta get "config") >> "publicKey") // Signed by my key (Easy access to config that added the handler, so can store the key there)
    ,
    !("expired" in (_x get "status")) // Not expired
    ,
    !("badDigest" in (_x get "status")) // The signature actually matches the binary (it was not edited after signing)
  ]
}) # 0;

private _isValid = ({ _x } count _validSignatureFound) isEqualTo count _validSignatureFound;

INFO_3("Extension '%1_x64.dll'(%2) validation finished. Result: %3.",_metaDta get "name",_metaDta get "fileVersion",_isValid);
if (!_isValid) then {
  _validSignatureFound params ["_validSignature","_notExpired","_verifyBinSignature"];
  ERROR_3("Extension validation failed. (ValidSignature=%1, notExpired=%2, verifyBinSignature=%3)",_validSignature,_notExpired,_verifyBinSignature);
};

_isValid // If signature found we load, otherwise we block
