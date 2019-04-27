using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static void Main(params string[] args) {

            var toEnable = args.Contains("enable");
            var toDisable = args.Contains("disable");
            var help = args.Contains("-h") || args.Contains("--help");

            if (toEnable == toDisable || help) {
                if (!help)
                    Console.Error.WriteLine("Invalid parameters");
                Console.WriteLine("Usage:");
                Console.WriteLine("  dark-skin.exe enable | disable [options]");
                Console.WriteLine("");
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