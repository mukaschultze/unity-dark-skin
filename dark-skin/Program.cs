using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DarkSkin {
    public static class Program {

        private const bool TO_ENABLE = true;

        private static void Main(params string[] args) {

            Console.Title = "Dark Skin for Unity";

            try {

                //var n = new UnitySkin(@"D:\Unity\2019.1.0a7-ImprovedUI\Editor\Unity.exe", true);
                //n.SetDarkSkinEnable(true);
                //return;

                Console.WriteLine("Fetching unity installations...");

                GetUnityInstallations(Environment.CurrentDirectory)
                    .AsParallel()
                    .Where(exe => File.Exists(exe))
                    .Select(exe => new UnitySkin(exe, false))
                    .Where(unity => unity.OffsetOfSkinFlags != -1 && unity.SkinIndex != -1)
                    .Where(unity => {
                        var shouldChange = (TO_ENABLE && unity.IsWhiteSkin) || (!TO_ENABLE && unity.IsDarkSkin);
                        if (!shouldChange)
                            unity.Log("Skin already applied, ignoring");
                        return shouldChange;
                    })
                    .ForAll(unity => unity.SetDarkSkinEnable(TO_ENABLE));

            } catch (Exception e) {
                Console.WriteLine("\nError");
                Console.WriteLine(e.Message);
            } finally {
                Console.WriteLine("\nFinished");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }

        }

        private static List<string> GetUnityInstallations(string root) {

            var folders = new List<string>();
            var unity = Path.Combine(root, "Unity.exe");

            if (File.Exists(unity)) {
                folders.Add(unity);
                return folders;
            }

            foreach (var directory in Directory.EnumerateDirectories(root))
                folders.AddRange(GetUnityInstallations(directory));

            return folders;
        }

    }
}