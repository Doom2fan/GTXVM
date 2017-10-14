namespace GTXVM {
    public enum GTXPCodes : byte {
        Nop = 0x00,

        /** Flow Control **/
        Jmp  = 0x01,
        CJmp = 0x02,

        /** Comparison **/
        Eq   = 0x03,
        Neq  = 0x04,
        ULt  = 0x05, // Unsigned comparison
        UGt  = 0x06,
        ULeq = 0x07,
        UGeq = 0x08,
        Lt   = 0x09, // Signed comparison
        Gt   = 0x0A,
        Leq  = 0x0B,
        Geq  = 0x0C,

        /** Arithmetics (On stack) **/
        UAdd = 0x0D, // Unsigned arithmetic
        USub = 0x0E,
        UMul = 0x0F,
        UDiv = 0x10,
        UMod = 0x11,
        UInc = 0x12,
        UDec = 0x13,
        Add  = 0x14, // Signed arithmetic
        Sub  = 0x15,
        Mul  = 0x16,
        Div  = 0x17,
        Mod  = 0x18,
        Inc  = 0x19,
        Dec  = 0x1A,
        KMul = 0x1B, // Fixed-point arithmetic
        KDiv = 0x1C,
        KMod = 0x1D,

        /** Arithmetics (On memory) **/
        UAddM = 0x1E, // Unsigned arithmetic
        USubM = 0x1F,
        UMulM = 0x20,
        UDivM = 0x21,
        UModM = 0x22,
        UIncM = 0x23,
        UDecM = 0x24,
        AddM  = 0x25, // Signed arithmetic
        SubM  = 0x26,
        MulM  = 0x27,
        DivM  = 0x28,
        ModM  = 0x29,
        IncM  = 0x2A,
        DecM  = 0x2B,
        KMulM = 0x2C, // Fixed-point arithmetic
        KDivM = 0x2D,
        KModM = 0x2E,

        /** Script Control **/
        Terminate       = 0x2F,
        Delay           = 0x30,
        CallSpecial     = 0x31,
        CallScript      = 0x32,
        CallNamedScript = 0x33,
        SWait           = 0x34,

        /** Stack Control **/
        Push    = 0x35,
        PushLit = 0x036,
        Pop     = 0x37,
        Peek    = 0x38,
        
        /** Memory Manipulation **/
        GStr   = 0x39,
        SetMem = 0x3A,
        Mov    = 0x3B,

        /** Bitwise Functions **/
        BShiftLeft      = 0x43, // Bitshifting
        BShiftRight     = 0x44,
        BShiftLeftSign  = 0x45,
        BShiftRightSign = 0x46,
        BitXOR          = 0x47,
        BitOR           = 0x48,
        BitAND          = 0x49,
        BitNOT          = 0x4A,
    }
}
