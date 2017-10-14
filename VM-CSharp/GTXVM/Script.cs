using GTXVM.Support;
using System;
using System.Linq;
using System.Text;

namespace GTXVM {
    public enum GTXScriptState {
        /// <summary>The script has just been initialized and hasn't started running yet</summary>
        Initialized = 0,
        /// <summary>The script is running</summary>
        Running,
        /// <summary>The script is delaying/sleeping</summary>
        Delayed,
        /// <summary>The script finished execution</summary>
        Terminated,
        /// <summary>The script did something that left it in an invalid state</summary>
        Invalid,
        /// <summary>The script exceeded the instructions per tic limit</summary>
        Runaway,
        /// <summary>The script divided by zero</summary>
        DivisionByZero,
        /// <summary>The script performed modulus by zero</summary>
        ModulusByZero,
    }
    
    public class GTXScriptData {
        protected byte [] code;
        protected uint codeOffs;
        protected uint entryPoint;
        protected uint ramSize;
        protected int stkSize;

        public byte [] Code {
            get { return code; }
            set { code = value; }
        }
        public uint CodeOffset {
            get { return entryPoint; }
            set { entryPoint = value; }
        }
        public uint EntryPoint {
            get { return entryPoint; }
            set { entryPoint = value; }
        }
        public uint MemorySize {
            get { return ramSize; }
            set { ramSize = value; }
        }
        public int StackSize {
            get { return stkSize; }
            set { stkSize = value; }
        }
        public event EventHandler OnStateChange = delegate { };
        
        /// <summary>
        /// Constructs a GTXScriptData
        /// </summary>
        /// <param name="cd">The script's machine code</param>
        /// <param name="offs">A pointer to where the script should be placed in memory</param>
        /// <param name="ePoint">The script's entry point</param>
        /// <param name="memSz">The script's memory size</param>
        public GTXScriptData (byte [] cd = null, uint offs = 0, uint ePoint = 0, uint memSz = 64 * 1024, int stkSz = 2500) {
            code = cd;
            codeOffs = offs;
            entryPoint = ePoint;
            ramSize = memSz;
            stkSize = stkSz;
        }
    }

    public class GTXScript {
        protected Stack<byte> stack;
        protected byte [] memory;
        protected uint ramSize;
        protected int stkSize;
        protected GTXScriptState state;
        protected uint codePointer;
        protected uint entryPoint;
        protected uint stateData;
        protected uint pID;
        protected GTXLibrary ownerLib;
        public event EventHandler OnStateChange = delegate { };

        /// <summary>
        /// Constructs a script with the specified amount of memory for usage by the script's code
        /// </summary>
        /// <param name="lib">The library the script belongs to</param>
        /// <param name="memSize">The amount of memory in bytes</param>
        public GTXScript (GTXLibrary lib, uint memSize = 64 * 1024, int stackSize = 2500) {
            ownerLib = lib;
            ramSize = memSize;
            memory = new byte [ramSize];
            memory.Initialize ();
            stack = new Stack<byte> (stackSize);
        }

        /// <summary>
        /// Gets the script's state
        /// </summary>
        public GTXScriptState State {
            get { return state; }
            set {
                state = value;
                OnStateChange (this, null);
            }
        }
        /// <summary>
        /// Gets the script's memory size
        /// </summary>
        public uint MemorySize { get { return ramSize; } }
        /// <summary>
        /// Gets the script's stack size
        /// </summary>
        public int StackSize { get { return stkSize; } }
        /// <summary>
        /// Gets or sets the script's code pointer
        /// </summary>
        public uint CodePointer {
            get { return codePointer; }
            set {
                if (value >= ramSize)
                    throw new IndexOutOfRangeException ("Code pointer value must point to somewhere in the script's memory");
                codePointer = value;
            }
        }
        /// <summary>
        /// Gets or sets the script's entry point
        /// </summary>
        public uint EntryPoint {
            get { return entryPoint; }
            set {
                if (value >= ramSize)
                    throw new IndexOutOfRangeException ("Entry point value must point to somewhere in the script's memory");
                entryPoint = value;
            }
        }
        /// <summary>
        /// Gets or sets the script's stack
        /// </summary>
        public Stack<byte> Stack {
            get { return stack; }
            set { stack = value; }
        }
        /// <summary>
        /// Gets or sets the script's library
        /// </summary>
        public GTXLibrary Library {
            get { return ownerLib; }
            set { ownerLib = value; }
        }
        /// <summary>
        /// Gets or sets the script's runtime ID
        /// </summary>
        public uint PID {
            get { return pID; }
            set { pID = value; }
        }

        /// <summary>
        /// Copies the specified amount of bytes from the script's memory at the specified location to the buffer
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the script's memory.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the script's memory.</param>
        /// <param name="pointer">The pointer to the memory location</param>
        /// <param name="count">The maximum number of bytes to be read from the script's memory.</param>
        public void GetFromMemory (byte [] buffer, uint offset, uint pointer, uint count) {
            if (buffer == null)
                throw new ArgumentNullException ("Buffer is null");
            if (buffer.Length < offset + count)
                throw new ArgumentException ("Buffer is smaller than offset + count");
            if (ramSize < pointer + count)
                throw new ArgumentException ("Pointer + count is outside the script's memory");

            Buffer.BlockCopy (memory, (int) pointer, buffer, (int) offset, (int) count);
        }
        /// <summary>
        /// Copies the specified amount of bytes from the script's memory at the specified location
        /// </summary>
        /// <param name="pointer">The pointer to the memory location</param>
        /// <param name="count">The maximum number of bytes to be read from the script's memory.</param>
        /// <returns>An array of bytes with the values</returns>
        public byte [] GetFromMemory (uint pointer, uint count) {
            if (ramSize < pointer + count)
                throw new ArgumentException ("Pointer + count is outside the script's memory");

            byte [] ret = new byte [count];
            Buffer.BlockCopy (memory, (int) pointer, ret, 0, (int) count);
            return ret;
        }
        /// <summary>
        /// Sets the memory at the specified location
        /// </summary>
        /// <param name="pointer">The pointer to the memory location</param>
        /// <param name="buffer">The values to set the memory to</param>
        /// <param name="count">The amount of bytes to set from the buffer</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        public void SetMemory (uint pointer, byte [] buffer, uint count = 0, uint offset = 0) {
            if (buffer.Length < offset + count)
                throw new ArgumentException ("Offset + count is outside the buffer");
            if (ramSize < pointer + count)
                throw new ArgumentException ("Pointer + count is outside the script's memory");

            if (count == 0)
                count = (uint) buffer.Length;

            Buffer.BlockCopy (buffer, (int) offset, memory, (int) pointer, (int) count);
        }
        /// <summary>
        /// Sets the code pointer to the specified location in memory
        /// </summary>
        /// <param name="pointer">The location in memory to jump to</param>
        protected void DoJump (uint pointer) {
            if (pointer >= ramSize) {
                State = GTXScriptState.Invalid;
                return;
            } else
                codePointer = pointer;
        }

        /// <summary>
        /// Runs the script
        /// </summary>
        /// <param name="execSingleOp">Whether to execute normally or only execute a single instruction (Useful for debugging)</param>
        public void Run (bool execSingleOp = false) {
#pragma warning disable CS0168 // Variable is declared but never used
            uint runaway = 0;
            uint uTMP1, uTMP2, uTMP3;
            int sTMP1, sTMP2, sTMP3;
#pragma warning restore CS0168 // Variable is declared but never used

            switch (state) {
                case GTXScriptState.Initialized:
                    State = GTXScriptState.Running;
                    break;

                case GTXScriptState.Delayed:
                    if (--stateData == 0)
                        State = GTXScriptState.Running;
                    break;
            }

            while (state == GTXScriptState.Running) {
                if (++runaway == 2000000 || codePointer >= ramSize) {
                    State = GTXScriptState.Runaway;
                    break;
                }

                switch ((GTXPCodes) memory [codePointer++]) {
                    case GTXPCodes.Nop:
                        // Do nothing
                        break;

                    /** Flow Control **/
                    case GTXPCodes.Jmp:
                        uTMP1 = PopInt (false); // pointer
                        DoJump (uTMP1);
                        break;

                    case GTXPCodes.CJmp:
                        uTMP2 = stack.Pop (); // condition
                        uTMP1 = PopInt (false); // pointer
                        if (uTMP2 > 0)
                            DoJump (uTMP1);
                        break;

                    /** Comparison **/
                    case GTXPCodes.Eq:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        stack.Push (uTMP2 == uTMP1 ? byte.MinValue : (byte) byte.MaxValue);
                        break;
                    case GTXPCodes.Neq:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        stack.Push (uTMP2 != uTMP1 ? byte.MinValue : (byte) byte.MaxValue);
                        break;
                    // Unsigned comparison
                    case GTXPCodes.ULt:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        stack.Push (uTMP2 < uTMP1 ? byte.MinValue : (byte) byte.MaxValue);
                        break;
                    case GTXPCodes.UGt:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        stack.Push (uTMP2 > uTMP1 ? byte.MinValue : (byte) byte.MaxValue);
                        break;
                    case GTXPCodes.ULeq:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        stack.Push (uTMP2 <= uTMP1 ? byte.MinValue : (byte) byte.MaxValue);
                        break;
                    case GTXPCodes.UGeq:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        stack.Push (uTMP2 >= uTMP1 ? byte.MinValue : (byte) byte.MaxValue);
                        break;
                    // Signed comparison
                    case GTXPCodes.Lt:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        stack.Push (sTMP2 < sTMP1 ? byte.MinValue : (byte) byte.MaxValue);
                        break;
                    case GTXPCodes.Gt:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        stack.Push (sTMP2 > sTMP1 ? byte.MinValue : (byte) byte.MaxValue);
                        break;
                    case GTXPCodes.Leq:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        stack.Push (sTMP2 <= sTMP1 ? byte.MinValue : (byte) byte.MaxValue);
                        break;
                    case GTXPCodes.Geq:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        stack.Push (sTMP2 >= sTMP1 ? byte.MinValue : (byte) byte.MaxValue);
                        break;

                    /** Arithmetics (On stack) **/
                    // Unsigned arithmetic
                    case GTXPCodes.UAdd:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        stack.Push (BitConverter.GetBytes (uTMP2 + uTMP1));
                        break;
                    case GTXPCodes.USub:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        stack.Push (BitConverter.GetBytes (uTMP2 - uTMP1));
                        break;
                    case GTXPCodes.UMul:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        stack.Push (BitConverter.GetBytes (uTMP2 * uTMP1));
                        break;
                    case GTXPCodes.UDiv:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        if (uTMP1 == 0)
                            State = GTXScriptState.DivisionByZero;
                        else
                            stack.Push (BitConverter.GetBytes (uTMP2 / uTMP1));
                        break;
                    case GTXPCodes.UMod:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        if (uTMP1 == 0)
                            State = GTXScriptState.ModulusByZero;
                        else
                            stack.Push (BitConverter.GetBytes (uTMP2 % uTMP1));
                        break;
                    case GTXPCodes.UInc:
                        uTMP1 = PopInt (false); // val
                        stack.Push (BitConverter.GetBytes (uTMP1++));
                        break;
                    case GTXPCodes.UDec:
                        uTMP1 = PopInt (false); // val
                        stack.Push (BitConverter.GetBytes (uTMP1--));
                        break;
                    // Signed arithmetic
                    case GTXPCodes.Add:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        stack.Push (BitConverter.GetBytes (sTMP2 + sTMP1));
                        break;
                    case GTXPCodes.Sub:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        stack.Push (BitConverter.GetBytes (sTMP2 - sTMP1));
                        break;
                    case GTXPCodes.Mul:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        stack.Push (BitConverter.GetBytes (sTMP2 * sTMP1));
                        break;
                    case GTXPCodes.Div:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        if (sTMP1 == 0)
                            State = GTXScriptState.DivisionByZero;
                        else
                            stack.Push (BitConverter.GetBytes (sTMP2 / sTMP1));
                        break;
                    case GTXPCodes.Mod:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        if (sTMP1 == 0)
                            State = GTXScriptState.ModulusByZero;
                        else
                            stack.Push (BitConverter.GetBytes (sTMP2 % sTMP1));
                        break;
                    case GTXPCodes.Inc:
                        sTMP1 = PopInt (true); // val
                        stack.Push (BitConverter.GetBytes (sTMP1++));
                        break;
                    case GTXPCodes.Dec:
                        sTMP1 = PopInt (true); // val
                        stack.Push (BitConverter.GetBytes (sTMP1--));
                        break;
                    // Fixed-point arithmetic
                    case GTXPCodes.KMul:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        stack.Push (BitConverter.GetBytes ((int) (((long) sTMP2 * sTMP1) >> 16)));
                        break;
                    case GTXPCodes.KDiv:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        if (sTMP1 == 0)
                            State = GTXScriptState.DivisionByZero;
                        else
                            stack.Push (BitConverter.GetBytes (FixedDiv (sTMP2, sTMP1)));
                        break;
                    case GTXPCodes.KMod:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        if (sTMP1 == 0)
                            State = GTXScriptState.ModulusByZero;
                        else
                            stack.Push (BitConverter.GetBytes (sTMP2 % sTMP1));
                        break;

                    /** Arithmetics (On memory) **/
                    // Unsigned arithmetic
                    case GTXPCodes.UAddM:
                        uTMP1 = BitConverter.ToUInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP3 = PopInt (false); // lhsPtr
                        uTMP2 = BitConverter.ToUInt32 (GetFromMemory (uTMP3, 4), 0); // lhs
                        SetMemory (uTMP3, BitConverter.GetBytes (uTMP2 + uTMP1));
                        break;
                    case GTXPCodes.USubM:
                        uTMP1 = BitConverter.ToUInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP3 = PopInt (false); // lhsPtr
                        uTMP2 = BitConverter.ToUInt32 (GetFromMemory (uTMP3, 4), 0); // lhs
                        SetMemory (uTMP3, BitConverter.GetBytes (uTMP2 - uTMP1));
                        break;
                    case GTXPCodes.UMulM:
                        uTMP1 = BitConverter.ToUInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP3 = PopInt (false); // lhsPtr
                        uTMP2 = BitConverter.ToUInt32 (GetFromMemory (uTMP3, 4), 0); // lhs
                        SetMemory (uTMP3, BitConverter.GetBytes (uTMP2 * uTMP1));
                        break;
                    case GTXPCodes.UDivM:
                        uTMP1 = BitConverter.ToUInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP3 = PopInt (false); // lhsPtr
                        uTMP2 = BitConverter.ToUInt32 (GetFromMemory (uTMP3, 4), 0); // lhs
                        if (uTMP1 == 0)
                            State = GTXScriptState.DivisionByZero;
                        else
                            SetMemory (uTMP3, BitConverter.GetBytes (uTMP2 / uTMP1));
                        break;
                    case GTXPCodes.UModM:
                        uTMP1 = BitConverter.ToUInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP3 = PopInt (false); // lhsPtr
                        uTMP2 = BitConverter.ToUInt32 (GetFromMemory (uTMP3, 4), 0); // lhs
                        if (uTMP1 == 0)
                            State = GTXScriptState.ModulusByZero;
                        else
                            SetMemory (uTMP3, BitConverter.GetBytes (uTMP2 % uTMP1));
                        break;
                    case GTXPCodes.UIncM:
                        uTMP2 = PopInt (false); // valPtr
                        uTMP1 = BitConverter.ToUInt32 (GetFromMemory (uTMP2, 4), 0); // val
                        SetMemory (uTMP2, BitConverter.GetBytes (uTMP1++));
                        break;
                    case GTXPCodes.UDecM:
                        uTMP2 = PopInt (false); // valPtr
                        uTMP1 = BitConverter.ToUInt32 (GetFromMemory (uTMP2, 4), 0); // val
                        SetMemory (uTMP2, BitConverter.GetBytes (uTMP1--));
                        break;
                    // Signed arithmetic
                    case GTXPCodes.AddM:
                        sTMP1 = BitConverter.ToInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP1 = PopInt (false); // lhsPtr
                        sTMP2 = BitConverter.ToInt32 (GetFromMemory (uTMP1, 4), 0); // lhs
                        SetMemory (uTMP1, BitConverter.GetBytes (sTMP2 + sTMP1));
                        break;
                    case GTXPCodes.SubM:
                        sTMP1 = BitConverter.ToInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP1 = PopInt (false); // lhsPtr
                        sTMP2 = BitConverter.ToInt32 (GetFromMemory (uTMP1, 4), 0); // lhs
                        SetMemory (uTMP1, BitConverter.GetBytes (sTMP2 - sTMP1));
                        break;
                    case GTXPCodes.MulM:
                        sTMP1 = BitConverter.ToInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP1 = PopInt (false); // lhsPtr
                        sTMP2 = BitConverter.ToInt32 (GetFromMemory (uTMP1, 4), 0); // lhs
                        SetMemory (uTMP1, BitConverter.GetBytes (sTMP2 * sTMP1));
                        break;
                    case GTXPCodes.DivM:
                        sTMP1 = BitConverter.ToInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP1 = PopInt (false); // lhsPtr
                        sTMP2 = BitConverter.ToInt32 (GetFromMemory (uTMP1, 4), 0); // lhs
                        if (sTMP1 == 0)
                            State = GTXScriptState.DivisionByZero;
                        else
                            SetMemory (uTMP1, BitConverter.GetBytes (sTMP2 / sTMP1));
                        break;
                    case GTXPCodes.ModM:
                        sTMP1 = BitConverter.ToInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP1 = PopInt (false); // lhsPtr
                        sTMP2 = BitConverter.ToInt32 (GetFromMemory (uTMP1, 4), 0); // lhs
                        if (sTMP1 == 0)
                            State = GTXScriptState.ModulusByZero;
                        else
                            SetMemory (uTMP1, BitConverter.GetBytes (sTMP2 % sTMP1));
                        break;
                    case GTXPCodes.IncM:
                        uTMP1 = PopInt (false); // valPtr
                        sTMP1 = BitConverter.ToInt32 (GetFromMemory (uTMP1, 4), 0); // val
                        SetMemory (uTMP1, BitConverter.GetBytes (sTMP1++));
                        break;
                    case GTXPCodes.DecM:
                        uTMP1 = PopInt (false); // valPtr
                        sTMP1 = BitConverter.ToInt32 (GetFromMemory (uTMP1, 4), 0); // val
                        SetMemory (uTMP1, BitConverter.GetBytes (sTMP1--));
                        break;
                    // Fixed-point arithmetic
                    case GTXPCodes.KMulM:
                        sTMP1 = BitConverter.ToInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP1 = PopInt (false); // lhsPtr
                        sTMP2 = BitConverter.ToInt32 (GetFromMemory (uTMP1, 4), 0); // lhs
                        stack.Push (BitConverter.GetBytes ((int) (((long) sTMP2 * sTMP1) >> 16)));
                        break;
                    case GTXPCodes.KDivM:
                        sTMP1 = BitConverter.ToInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP1 = PopInt (false); // lhsPtr
                        sTMP2 = BitConverter.ToInt32 (GetFromMemory (uTMP1, 4), 0); // lhs
                        if (sTMP1 == 0)
                            State = GTXScriptState.DivisionByZero;
                        else
                            stack.Push (BitConverter.GetBytes (FixedDiv (sTMP2, sTMP1)));
                        break;
                    case GTXPCodes.KModM:
                        sTMP1 = BitConverter.ToInt32 (GetFromMemory (PopInt (false), 4), 0); // rhs
                        uTMP1 = PopInt (false); // lhsPtr
                        sTMP2 = BitConverter.ToInt32 (GetFromMemory (uTMP1, 4), 0); // lhs
                        if (sTMP1 == 0)
                            State = GTXScriptState.ModulusByZero;
                        else
                            stack.Push (BitConverter.GetBytes (sTMP2 % sTMP1));
                        break;

                    /** Script Control **/
                    case GTXPCodes.Terminate:
                        State = GTXScriptState.Terminated;
                        break;
                    case GTXPCodes.Delay:
                        stateData = PopInt (false); // time
                        State = GTXScriptState.Delayed;
                        break;

                    case GTXPCodes.CallSpecial:
                        uTMP1 = PopInt (false); // idx
                        uTMP2 = PopInt (false); // amount
                        ownerLib.Specials [uTMP1] (this, stack.PopReverse (uTMP2));
                        break;
                    case GTXPCodes.CallScript:
                        uTMP1 = PopInt (false); // idx
                        uTMP2 = PopInt (false); // amount
                        ownerLib.StartScript (uTMP1, stack.PopReverse (uTMP2));
                        break;
                    case GTXPCodes.CallNamedScript:
                        uTMP1 = PopInt (false); // strId
                        uTMP2 = PopInt (false); // amount
                        if (!ownerLib.StringTable.ContainsKey (uTMP2))
                            State = GTXScriptState.Invalid;
                        else
                            ownerLib.StartNamedScript (ownerLib.StringTable [uTMP1], stack.PopReverse (uTMP2));
                        break;
                    case GTXPCodes.SWait: {
                            uTMP1 = PopInt (false); // pID
                            GTXScript sc = ownerLib.GetRunningScript (uTMP1);
                            if (sc != null) {
                                State = GTXScriptState.Delayed;
                                uint id = pID;
                                sc.OnStateChange += delegate {
                                    GTXScript script = ownerLib.GetRunningScript (id);
                                    script.State = GTXScriptState.Running;
                                };
                            }
                        }
                        break;

                    /** Stack Control **/
                    case GTXPCodes.Push:
                        uTMP2 = PopInt (false); // pointer
                        uTMP1 = PopInt (false); // amount
                        stack.Push (GetFromMemory (uTMP2, uTMP1));
                        break;

                    case GTXPCodes.PushLit:
                        uTMP1 = BitConverter.ToUInt32 (GetFromMemory (codePointer, 4), 0); // amount
                        stack.Push (GetFromMemory (codePointer + 4, uTMP1));
                        codePointer += uTMP1 + 4;
                        break;

                    case GTXPCodes.Pop:
                        uTMP2 = PopInt (false); // pointer
                        uTMP1 = PopInt (false); // amount
                        SetMemory (uTMP2, stack.PopReverse (uTMP1));
                        break;

                    case GTXPCodes.Peek:
                        uTMP2 = PopInt (false); // pointer
                        uTMP1 = PopInt (false); // amount
                        SetMemory (uTMP2, stack.PopReverse (uTMP1));
                        break;

                    /** Memory Manipulation **/
                    case GTXPCodes.GStr:
                        uTMP1 = PopInt (false); // pointer
                        uTMP2 = PopInt (false); // strId
                        if (!ownerLib.StringTable.ContainsKey (uTMP2))
                            State = GTXScriptState.Invalid;
                        else
                            SetMemory (uTMP1, Encoding.ASCII.GetBytes (ownerLib.StringTable [uTMP2]));
                        break;
                    case GTXPCodes.SetMem:
                        sTMP1 = GetFromMemory (codePointer++, 1) [0]; // which

                        if (sTMP1 > 0) {
                            State = GTXScriptState.Invalid; // Not implemented
                            break;
                        }

                        uTMP1 = PopInt (false); // amount
                        uTMP2 = PopInt (false); // pointer
                        Buffer.BlockCopy (memory, (int) codePointer, memory, (int) uTMP2, (int) uTMP1); // Not using SetMemory and GetFromMemory here for performance
                        codePointer += uTMP1;
                        break;
                    case GTXPCodes.Mov:
                        sTMP3 = GetFromMemory (codePointer++, 1) [0]; // which byte
                        sTMP1 = (sTMP3 >> 4) & 0x0F; // src
                        sTMP2 = sTMP3 & 0x0F; // dst

                        if ((sTMP1 | sTMP2) > 0) {
                            State = GTXScriptState.Invalid; // Not implemented
                            break;
                        }

                        uTMP1 = PopInt (false); // amount
                        uTMP2 = PopInt (false); // dstPointer
                        uTMP3 = PopInt (false); // srcPointer
                        Buffer.BlockCopy (memory, (int) uTMP3, memory, (int) uTMP2, (int) uTMP1); // Not using SetMemory and GetFromMemory here for performance
                        break;

                    /** Bitwise functions **/
                    case GTXPCodes.BShiftLeft: // Bitshifting
                        sTMP1 = PopInt (true);  // rhs
                        uTMP1 = PopInt (false); // lhs
                        stack.Push (BitConverter.GetBytes (uTMP1 << sTMP1));
                        break;
                    case GTXPCodes.BShiftRight:
                        sTMP1 = PopInt (true);  // rhs
                        uTMP1 = PopInt (false); // lhs
                        stack.Push (BitConverter.GetBytes (uTMP1 >> sTMP1));
                        break;
                    case GTXPCodes.BShiftLeftSign:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        stack.Push (BitConverter.GetBytes (sTMP2 << sTMP1));
                        break;
                    case GTXPCodes.BShiftRightSign:
                        sTMP1 = PopInt (true); // rhs
                        sTMP2 = PopInt (true); // lhs
                        stack.Push (BitConverter.GetBytes (sTMP2 >> sTMP1));
                        break;
                    case GTXPCodes.BitXOR:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        stack.Push (BitConverter.GetBytes (uTMP2 ^ uTMP1));
                        break;
                    case GTXPCodes.BitOR:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        stack.Push (BitConverter.GetBytes (uTMP2 | uTMP1));
                        break;
                    case GTXPCodes.BitAND:
                        uTMP1 = PopInt (false); // rhs
                        uTMP2 = PopInt (false); // lhs
                        stack.Push (BitConverter.GetBytes (uTMP2 & uTMP1));
                        break;
                    case GTXPCodes.BitNOT:
                        uTMP1 = PopInt (false); // val
                        stack.Push (BitConverter.GetBytes (~uTMP1));
                        break;

                    default:
                        State = GTXScriptState.Invalid; // Not implemented or non-existant
                        break;
                }
                
                if (execSingleOp)
                    break;
            }
        }

        /// <summary>
        /// Pops an int from the stack
        /// </summary>
        protected dynamic PopInt (bool signed = false) {
            byte [] bytes = (byte []) (stack.Pop (4).Reverse ());
            if (signed)
                return BitConverter.ToInt32 (bytes, 0);
            else
                return BitConverter.ToUInt32 (bytes, 0);
        }

        /// <summary>
        /// Clears the script's memory
        /// </summary>
        public void ClearMemory () {
            for (int i = 0; i < ramSize; i++)
                memory [i] = 0;
        }
        /// <summary>
        /// Empties the script's stack
        /// </summary>
        public void ClearStack () {
            stack.Clear ();
        }
        /// <summary>
        /// Resets the script to its initial state
        /// </summary>
        public void Reset () {
            ClearMemory ();
            ClearStack ();
            codePointer = entryPoint;
            State = GTXScriptState.Initialized;
            stateData = 0;
        }

        protected static int FixedDiv (int a, int b) {
            if ((uint) (System.Math.Abs (a)) >> 16 >= (uint) System.Math.Abs (b))
                return (a ^ b) < 0 ? unchecked((int) 0x80000000) : 0x7fffffff;

            return FixedDiv2 (a, b);
        }
        protected static int FixedDiv2 (int a, int b) { return (int) (((long) a << 16) / b); }
    }
}
