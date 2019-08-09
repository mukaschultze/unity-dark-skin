using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CodeProject;

namespace DarkSkin {
    public static class Program {

        private static bool m_toEnable = true;
        private static bool m_useFastFileEnumerator = false;

        public static ParallelQuery<TSource> Tap<TSource>(this ParallelQuery<TSource> source, Action<TSource> action) {
            return source.Select(item => {
                action(item);
                return item;
            });
        }

        public static IEnumerable<TSource> Tap<TSource>(this IEnumerable<TSource> source, Action<TSource> action) {
            return source.Select(item => {
                action(item);
                return item;
            });
        }

        private static void FindHex(string unity) {

            using(var cvDump = new CVDump()) {
                var regex = new Regex(@"^.+:\s*\[(?<section>[0-9a-fA-F]{4}):(?<addr>[0-9a-fA-F]{8})\].*GetSkinIdx.*$");

                cvDump.Execute(unity,
                    (args, e) => { // Stdout
                        if (string.IsNullOrWhiteSpace(e.Data))
                            return;

                        var match = regex.Match(e.Data);

                        if (match.Success) {
                            var section = match.Groups["section"];
                            var addr = long.Parse(match.Groups["addr"].ToString(), System.Globalization.NumberStyles.HexNumber);

                            addr += 0x400; // Section offset

                            Console.WriteLine(e.Data);

                            using(new TempConsoleColor(ConsoleColor.DarkGreen))
                            Console.WriteLine("Found GetSkinIdx at section {0} and address {1:X8}", section, addr);

                            try {
                                using(var file = File.OpenRead(unity)) {
                                    var buffer = new byte[0x80]; // 45 is a random number that might be enought

                                    file.Seek(addr, SeekOrigin.Begin);
                                    file.Read(buffer, 0, buffer.Length);

                                    var formattedBytes = UnitySkin.FormatBytes(buffer);
                                    Console.WriteLine("x64 Routine:");
                                    Console.WriteLine(formattedBytes);
                                }
                            } catch (Exception ex) {
                                Console.Error.WriteLine("Failed to open Unity executable");
                                Console.Error.WriteLine(ex);
                            }
                        }
                    }, (args, e) => { // Stderr
                        Console.Error.WriteLine(e.Data);
                    });

            }
        }

        private static void Main(params string[] args) {

            var findHex = args.Contains("findHex");
            var unityArg = findHex ? args[Array.IndexOf(args, "findHex") + 1] : "";

            if (findHex) {
                FindHex(unityArg);
                return;
            }

            var toEnable = args.Contains("enable");
            var toDisable = args.Contains("disable");
            var help = args.Contains("-h") || args.Contains("--help");

            if (toEnable == toDisable || help) {
                if (!help)
                    Console.Error.WriteLine("Invalid parameters");
                Console.WriteLine("Usage:");
                Console.WriteLine("  dark-skin.exe enable | disable [options]");
                Console.WriteLine("");
                Console.WriteLine("    findHex unityExe   Find the address of the GetSkinIdx method for a particular Unity version");
                Console.WriteLine("-h, --help             Show this screen");
                Console.WriteLine("-f, --fast-enumerator  Use fast file enumeration, otherwise use recursive enumeration");
                return;
            }

            m_toEnable = toEnable;
            m_useFastFileEnumerator = args.Contains("-f") || args.Contains("--fast-enumerator");

            Console.Title = "Dark Skin for Unity";

            try {

                // try {
                //     var exeBytes = File.ReadAllBytes(@"C:\Unity\2019.2.0a11\Editor\Unity.exe");
                //     var functionOffset = 0x00AB6CF0;
                //     var baseAddr = 0x400;
                //     Console.WriteLine(UnitySkin.FormatBytes(exeBytes, functionOffset + baseAddr, 1000));
                // } catch (Exception e) {
                //     Console.WriteLine(e);
                // } finally {
                //     Console.ReadLine();
                // }

                Console.WriteLine("Fetching unity installations...");

                GetUnityInstallations(Environment.CurrentDirectory)
                    .AsParallel()
                    .Where(exe => File.Exists(exe))
                    .Select(exe => new UnitySkin(exe))
                    .Where(unity => unity.OffsetOfSkinFlags != -1 && unity.SkinIndex != -1)
                    .Where(unity => {
                        var shouldChange = (toEnable && unity.IsWhiteSkin) || (!toEnable && unity.IsDarkSkin);
                        if (!shouldChange)
                            unity.Log("Skin already applied, ignoring");
                        return shouldChange;
                    })
                    .ForAll(unity => unity.SetDarkSkinEnable(toEnable));

            } catch (Exception e) {
                Console.Error.WriteLine("\nError");
                Console.Error.WriteLine(e);
            }
        }

        private static List<string> GetUnityInstallations(string root) {

            if (m_useFastFileEnumerator)
                return FastDirectoryEnumerator.EnumerateFiles(root, "Unity.exe", SearchOption.AllDirectories)
                    .AsParallel()
                    .Tap(file => Console.WriteLine("Found {0}", file.Path))
                    .Select(file => file.Path)
                    .ToList();

            var folders = new List<string>();
            var unity = Path.Combine(root, "Unity.exe");

            if (File.Exists(unity)) {
                Console.WriteLine("Found {0}", unity);
                folders.Add(unity);
                return folders;
            }

            foreach (var directory in Directory.EnumerateDirectories(root))
                folders.AddRange(GetUnityInstallations(directory));

            return folders;
        }

    }
}