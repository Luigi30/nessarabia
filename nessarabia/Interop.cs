using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace nessarabia
{
    public class Interop
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct InteropProcessorStatus
        {
            public ushort cycles;           //cycles
            public byte FLAG_SIGN;          //N
            public byte FLAG_OVERFLOW;      //V
                                            //-
            public byte FLAG_BREAKPOINT;    //B
            public byte FLAG_DECIMAL;       //D
            public byte FLAG_INTERRUPT;     //I
            public byte FLAG_ZERO;          //Z
            public byte FLAG_CARRY;         //C

            public byte accumulator;        //A
            public byte index_x;            //X
            public byte index_y;            //Y
            public byte stack_pointer;      //SP
            public ushort program_counter;  //PC
            public ushort old_program_counter;  //before this opcode ran

            IntPtr last_opcode;
            public string lastOpcodeAsString { get { return Marshal.PtrToStringAnsi(last_opcode); } }
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MC6821Status
        {
            public byte KBD;
            public byte KBDCR;
            public byte DSP;
            public byte DSPCR;
        };

        [DllImport("./M6502EmulatorDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool loadBinary([MarshalAs(UnmanagedType.LPStr)] string path, ushort address);

        [DllImport("./M6502EmulatorDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool loadBinaryData(IntPtr data, Int32 size, ushort address);

        [DllImport("./M6502EmulatorDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort getProgramCounter();

        [DllImport("./M6502EmulatorDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern InteropProcessorStatus getProcessorStatus();

        [DllImport("./M6502EmulatorDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void resetProcessor();

        //Execution
        [DllImport("./M6502EmulatorDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void doSingleStep();

        [DllImport("./M6502EmulatorDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern MC6821Status getMC6821Status();

        [DllImport("./M6502EmulatorDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte getOutputBuffer();

        [DllImport("./M6502EmulatorDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getMemoryRange(ushort baseAddr, ushort length);

        [DllImport("./M6502EmulatorDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void resetCycleCounter();

        [DllImport("./M6502EmulatorDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void putKeyInBuffer(byte key);
    }
}
