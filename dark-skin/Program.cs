using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CodeProject;
using CommandLine;

namespace DarkSkin {
    public static class Program {

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

        private static void FindHex(string unityPath, bool useFastFileEnumerator) {

            if (useFastFileEnumerator)
                using(new TempConsoleColor(ConsoleColor.DarkYellow))
            Console.WriteLine("Using fast file enumeration");

            using(var cvDump = new CVDump()) {
                var obj = new object();

                GetUnityInstallations(Path.GetFullPath(unityPath), useFastFileEnumerator)
                    .AsParallel()
                    .Where(exe => File.Exists(exe))
                    .ForAll(unity => {
                        cvDump.Execute(unity,
                            (args, e) => { // Stdout
                                if (string.IsNullOrWhiteSpace(e.Data))
                                    return;

                                var regex = new Regex(@"^.+:\s*\[(?<section>[0-9a-fA-F]{4}):(?<addr>[0-9a-fA-F]{8})\].*GetSkinIdx.*$");
                                var match = regex.Match(e.Data);

                                if (match.Success)
                                    lock(obj) {
                                        var section = match.Groups["section"];
                                        var addr = long.Parse(match.Groups["addr"].ToString(), System.Globalization.NumberStyles.HexNumber);

                                        addr += 0x400; // Section offset

                                        Console.WriteLine(e.Data);

                                        using(new TempConsoleColor(ConsoleColor.DarkGreen))
                                        Console.WriteLine("Found GetSkinIdx at section {0} and address {1:X8}", section, addr);

                                        try {
                                            using(var file = File.OpenRead(unity)) {
                                                var buffer = new byte[0x80 * 5]; // This should be enough to reach the "ret" op

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
                                using(new TempConsoleColor(ConsoleColor.DarkRed))
                                Console.Error.WriteLine(e.Data);
                            });
                    });
            }
        }

        private static void Run(bool toEnable, string unityPath, bool useFastFileEnumerator) {
            Console.WriteLine("Fetching unity installations...");

            if (useFastFileEnumerator)
                using(new TempConsoleColor(ConsoleColor.DarkYellow))
            Console.WriteLine("Using fast file enumeration");

            GetUnityInstallations(Path.GetFullPath(unityPath), useFastFileEnumerator)
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
        }

        private static void HandleException(Exception ex) {
            using(new TempConsoleColor(ConsoleColor.DarkRed)) {
                Console.Error.WriteLine("\nError");
                Console.Error.WriteLine(ex);
            }
        }

        private static int Main(params string[] args) {

            Console.Title = "Dark Skin for Unity";

            return Parser.Default.ParseArguments<EnableOptions, DisableOptions, FindHexOptions>(args)
                .MapResult(
                    (EnableOptions opts) => {
                        try {
                            Run(true, opts.InputFile, opts.FastEnumerator);
                            return 0;
                        } catch (Exception ex) {
                            HandleException(ex);
                            return 1;
                        }
                    },
                    (DisableOptions opts) => {
                        try {
                            Run(false, opts.InputFile, opts.FastEnumerator);
                            return 0;
                        } catch (Exception ex) {
                            HandleException(ex);
                            return 1;
                        }
                    },
                    (FindHexOptions opts) => {
                        try {
                            FindHex(opts.InputFile, opts.FastEnumerator);
                            return 0;
                        } catch (Exception ex) {
                            HandleException(ex);
                            return 1;
                        }
                    },
                    (errs) => 1);
        }

        private static List<string> GetUnityInstallations(string root, bool useFastFileEnumerator) {

            if (useFastFileEnumerator)
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
                folders.AddRange(GetUnityInstallations(directory, useFastFileEnumerator));

            return folders;
        }

    }
}