using System;

namespace RomajiTyping
{
    public enum ConversionSearchMode : byte
    {
        //If None, only search for Any
        None = 0,
        MicroSoft = 1,
        MS = MicroSoft,
        Google = 2,

        //If Any, search for both MS and Google
        Any = MS | Google,
    }

    [Flags]
    public enum ConversionMode : byte
    {
        None = 0,
        MicroSoft = 1,
        MS = MicroSoft,
        Google = 2,

        //Available for both MS and Google
        Any = MS | Google,
    }
}