using System;
using System.Runtime.InteropServices;

namespace GTXVMTestUI {
    public class Utils {
        #region Byte array to hex (Messy!)
        private static readonly uint [] _byteToHexLookup32 = CreateByteToHexLookup32 ();
#if UNSAFE
        private unsafe static readonly uint* _byteToHexLookup32P = (uint*) GCHandle.Alloc (_byteToHexLookup32, GCHandleType.Pinned).AddrOfPinnedObject ();
#endif

        private static uint [] CreateByteToHexLookup32 () {
            var result = new uint [256];

            for (int i = 0; i < 256; i++) {
                string s = i.ToString ("X2");

#if UNSAFE
                if (BitConverter.IsLittleEndian)
                    result [i] = ((uint) s [0]) + ((uint) s [1] << 16);
                else
                    result [i] = ((uint) s [1]) + ((uint) s [0] << 16);
#else
                result [i] = ((uint) s [0]) + ((uint) s [1] << 16);
#endif
            }

            return result;
        }

        public static string ByteArrayToHex (byte [] bytes) {
            var result = new char [bytes.Length * 2];

#if UNSAFE
            unsafe {
                var lookupP = _byteToHexLookup32P;

                fixed (byte* bytesP = bytes) {
                    fixed (char* resultP = result) {
                        uint* resultP2 = (uint*) resultP;
                        for (int i = 0; i < bytes.Length; i++)
                            resultP2 [i] = lookupP [bytesP [i]];
                    }
                }
            }
#else
            var lookup32 = _byteToHexLookup32;

            for (int i = 0; i < bytes.Length; i++) {
                var val = lookup32 [bytes [i]];
                result [2 * i] = (char) val;
                result [2 * i + 1] = (char) (val >> 16);
            }
#endif

            return new string (result);
        }
        #endregion
    }
}