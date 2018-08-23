using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DarkSkin {
    public class UnitySkin {

        public const string WHITE_HEX = "75 08 33 C0 48 83 C4 20 5B C3 8B 03 48 83 C4 20 5B C3 CC CC CC CC CC CC";
        public const string DARK_HEX = "74 08 33 C0 48 83 C4 20 5B C3 8B 03 48 83 C4 20 5B C3 CC CC CC CC CC CC";

        public static readonly byte[] whiteBytes = GetBytesFromHex(WHITE_HEX);
        public static readonly byte[] darkBytes = GetBytesFromHex(DARK_HEX);

        public string UnityExe { get; private set; }
        public int AddressOfSkinFlags { get; private set; }
        public bool IsDarkSkin { get; private set; }
        public bool IsWhiteSkin { get; private set; }

        public UnitySkin(string unityExe) {
            var exeBytes = File.ReadAllBytes(unityExe);

            UnityExe = unityExe;
            AddressOfSkinFlags = GetAddressOfSkinFlag(exeBytes);

            if (AddressOfSkinFlags != -1) {
                IsDarkSkin = ArrayMatch(exeBytes, AddressOfSkinFlags, darkBytes);
                IsWhiteSkin = ArrayMatch(exeBytes, AddressOfSkinFlags, whiteBytes);
            }
        }

        public void SetDarkSkinEnable(bool enable) {
            using (var stream = File.Open(UnityExe, FileMode.Open)) {
                stream.Position = AddressOfSkinFlags;
                stream.Write(enable ? darkBytes : whiteBytes, 0, (enable ? darkBytes : whiteBytes).Length);
            }
        }

        private static byte[] GetBytesFromHex(string hex) {
            return (from b in hex.Split(' ')
                    select byte.Parse(b, NumberStyles.HexNumber)).ToArray();
        }

        private static bool ArrayMatch(byte[] array, int offset, byte[] check) {
            for (int i = 0, j = offset; i < check.Length; i++, j++)
                if (j >= array.Length || j < 0 || array[j] != check[i])
                    return false;

            return true;
        }

        private int GetAddressOfSkinFlag(byte[] exeBytes) {
            if (whiteBytes.Length != darkBytes.Length)
                throw new InvalidOperationException("Dark and white skin hex lengh don't match");

            for (var i = 0; i < exeBytes.Length; i++)
                for (var j = 0; j < whiteBytes.Length; j++)
                    if (i + j > exeBytes.Length)
                        return -1; // There's no more space to have any matches.
                    else if (exeBytes[i + j] != whiteBytes[j] && exeBytes[i + j] != darkBytes[j])
                        break; // Our byte is different, continue to next one.
                    else if (j == whiteBytes.Length - 1)
                        return i; // We matched all the bytes, return the address.

            return -1;
        }

    }
}
