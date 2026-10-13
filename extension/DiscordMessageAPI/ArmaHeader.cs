#if WINDOWS
global using CallConv = System.Runtime.CompilerServices.CallConvStdcall;
#else
global using CallConv = System.Runtime.CompilerServices.CallConvCdecl;
#endif
