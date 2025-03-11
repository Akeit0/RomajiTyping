using System;

namespace RomajiTyping
{
   
    public enum ConversionSearchMode:byte
    {
        None = 0,
        MicroSoft = 1,
        MS = MicroSoft,
        Google=2,
        Any = MS | Google,
        All = 4,
    }
    [Flags]
    public enum ConversionMode:byte
    {
        None = 0,
        MicroSoft = 1,
        MS = MicroSoft,
        Google=2,
        Any = MS | Google,
    }
}