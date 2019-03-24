using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DarkSkin {
    public class UnitySkin {

        public static readonly string[] WHITE_HEX = new[] {
            "84 C0 75 08 33 C0 48 83 C4 20 5B C3 8B 03 48 83 C4 20 5B C3", // <= 2018.2
            "84 C0 75 08 33 C0 48 83 C4 30 5B C3 8B 03 48 83 C4 30 5B C3", // == 2018.3 
            "84 DB 74 04 33 C0 EB 02 8B 07", // >= 2019.1
            // "74 56 48 8B 7C 24 30 48 83 7C 24 38 00 77 2F 48 85 FF 74 2A 48 8B 74 24 48 48 8B 0B 48 85 C9 74 10 48 83 7B 08 00 76 09 48 8D 53"
        };
        public static readonly string[] DARK_HEX = new[] {
            "84 C0 74 08 33 C0 48 83 C4 20 5B C3 8B 03 48 83 C4 20 5B C3", // <= 2018.2 
            "84 C0 74 08 33 C0 48 83 C4 30 5B C3 8B 03 48 83 C4 30 5B C3", // == 2018.3 
            "84 DB 75 04 33 C0 EB 02 8B 07", // >= 2019.1
            // "74 56 48 8B 7C 24 30 48 83 7C 24 38 00 77 2F 48 85 FF 74 2A 48 8B 74 24 48 48 8B 0B 48 85 C9 74 10 48 83 7B 08 00 76 09 48 8D 53"
        };

        public static readonly byte[][] whiteBytes = GetBytesFromHex(WHITE_HEX);
        public static readonly byte[][] darkBytes = GetBytesFromHex(DARK_HEX);

        public string UnityExe { get; private set; }
        public string UnityBackupExe { get { return Path.ChangeExtension(UnityExe, ".backup.exe"); } }

        public int OffsetOfSkinFlags { get; private set; } = -1;
        public int SkinIndex { get; private set; } = -1;

        public bool IsDarkSkin { get; private set; }
        public bool IsWhiteSkin { get; private set; }

        public UnitySkin(string unityExe, bool checkMatches) {

            UnityExe = unityExe;

            if (checkMatches) {
                var exeBytes = File.ReadAllBytes(unityExe);
                BestMatches(exeBytes);
                return;
            }

            if (whiteBytes.Length != darkBytes.Length)
                throw new Exception("Non matching number of skins hexes");

            var skinSequence = new byte[0];

            using (var stream = File.OpenRead(unityExe))
                OffsetOfSkinFlags = GetOffsetOfSkinFlag(stream, darkBytes.Concat(whiteBytes).ToArray(), out skinSequence);

            if (OffsetOfSkinFlags != -1) {
                IsDarkSkin = darkBytes.Contains(skinSequence);
                IsWhiteSkin = whiteBytes.Contains(skinSequence);

                if (IsDarkSkin)
                    SkinIndex = Array.IndexOf(darkBytes, skinSequence);
                else if (IsWhiteSkin)
                    SkinIndex = Array.IndexOf(whiteBytes, skinSequence);
                else
                    Log("WUT");

                //Log("Current skin {0} (index {1}, offset 0x{2:X8})", IsDarkSkin ? "dark" : IsWhiteSkin ? "white" : "none", SkinIndex, OffsetOfSkinFlags);
            } else
                Log("Invalid executable, skin bytes not found");
        }

        public void SetDarkSkinEnable(bool enable) {
            EnsureBackup();

            try {
                Log("Applying {0} skin...", enable ? "dark" : "white");
                using (var stream = File.Open(UnityExe, FileMode.Open)) {
                    stream.Position = OffsetOfSkinFlags;
                    // stream.Position = 0x011E1FB0;
                    // SkinIndex = 3;
                    stream.Write(enable ? darkBytes[SkinIndex] : whiteBytes[SkinIndex], 0, (enable ? darkBytes[SkinIndex] : whiteBytes[SkinIndex]).Length);
                }
                //Log("Success!");
            } catch (Exception e) {
                RestoreBackup();
                throw e;
            }
        }

        private static byte[][] GetBytesFromHex(string[] hexes) {
            var results = new byte[hexes.Length][];
            for (var i = 0; i < hexes.Length; i++) {
                results[i] = GetBytesFromHex(hexes[i]);
            }
            return results;
        }

        private static byte[] GetBytesFromHex(string hex) {
            return hex.Split(' ').Select(b => byte.Parse(b, NumberStyles.HexNumber)).ToArray();
        }

        private static void BestMatches(byte[] exeBytes) {
            for (var i = 0; i < whiteBytes.Length; i++) {
                Console.WriteLine("\nTesting white skin sequence {0}", i);
                BestMatches(exeBytes, whiteBytes[i]);
            }
            for (var i = 0; i < darkBytes.Length; i++) {
                Console.WriteLine("\nTesting dark skin sequence {0}", i);
                BestMatches(exeBytes, darkBytes[i]);
            }
        }

        private static void BestMatches(byte[] exeBytes, byte[] skinBytes) {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine("Sequence:\t\t{0}", FormatBytes(skinBytes));
            var c = 0x011E1FB0;
            var cs = MatchPercent(exeBytes, c, skinBytes);
            Console.WriteLine("0x{0:X8} ({1:00.0%}):\t{2} - {3:0.00%}", c, (float)c / exeBytes.Length, FormatBytes(exeBytes, c, 1000), cs);
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Parallel.For(c, c + 5000 /*exeBytes.Length*/ , (i) => {
                var match = MatchPercent(exeBytes, i, skinBytes);
                if (match > 0.4)
                    Console.WriteLine("0x{0:X8} ({1:00.0%}):\t{2} - {3:0.00%}", i, (float)i / exeBytes.Length, FormatBytes(exeBytes, i, skinBytes.Length), match);
            });
        }

        private static float MatchPercent(byte[] exeBytes, int offset, byte[] skinBytes) {
            var matches = 0;

            for (int i = 0, j = offset; i < skinBytes.Length && j < exeBytes.Length; i++, j++)
                if (exeBytes[j] == skinBytes[i])
                    matches++;

            return (float)matches / skinBytes.Length;
        }

        private static int GetOffsetOfSkinFlag(Stream fileStream, byte[][] sequences, out byte[] matchedSequence) {

            var buffer = new byte[fileStream.Length];

            void ensureFileAddrIsAvailable(int index) {
                const int READ_BYTES = 8 * 1024; // 1KB

                if (index >= fileStream.Position)
                    fileStream.Read(buffer, (int)fileStream.Position, Math.Min(READ_BYTES, buffer.Length - (int)fileStream.Position));
            }

            for (var addr = 0; addr < buffer.Length; addr++) {
                ensureFileAddrIsAvailable(addr + 1000);
                for (var i = 0; i < sequences.Length; i++) {
                    matchedSequence = sequences[i];

                    if (SequenceEquals(buffer, addr, matchedSequence, 0, matchedSequence.Length))
                        return addr;
                }
            }

            matchedSequence = null;
            return -1;
        }

        private static bool SequenceEquals(byte[] a, int offsetA, byte[] b, int offsetB, int count) {

            if (offsetA + count > a.Length || offsetB + count > b.Length)
                return false;

            for (var i = 0; i < count; i++)
                if (a[i + offsetA] != b[i + offsetB])
                    return false;

            return true;
        }

        private void EnsureBackup() {
            if (File.Exists(UnityBackupExe))
                return;

            // Log("Backing up...", UnityExe);
            var bytes = File.ReadAllBytes(UnityExe);
            File.WriteAllBytes(UnityBackupExe, bytes);
        }

        private void RestoreBackup() {
            if (!File.Exists(UnityExe) || !File.Exists(UnityBackupExe))
                return;

            Log("Restoring backup...");
            File.Delete(UnityExe);
            var bytes = File.ReadAllBytes(UnityBackupExe);
            File.WriteAllBytes(UnityExe, bytes);
        }

        public static string FormatBytes(byte[] bytes) {
            return string.Join(" ", bytes.Select(b => b.ToString("X2")));
        }

        public static string FormatBytes(byte[] bytes, int skip, int take) {
            return string.Join(" ", bytes.Skip(skip).Take(take).Select(b => b.ToString("X2")));
        }

        public void Log(string format, params object[] args) {
            Console.WriteLine("{0}: {1}", UnityExe.Replace("\\Editor\\Unity.exe", string.Empty), string.Format(format, args));
        }

    }
}