using System;
using System.IO;
using System.Linq;

namespace DarkSkin {
    public static class Program {

        private const bool TO_ENABLE = true;

        private static void Main(params string[] args) {

            Console.Title = "Dark Skin for Unity";

            try {
                Console.WriteLine("Fetching unity installations...");

                var unitys = Directory.EnumerateDirectories(Environment.CurrentDirectory, "*", SearchOption.AllDirectories)
                   .Select(dir => Path.Combine(dir, "Unity.exe"))
                   .Where(exe => {
                       var exists = File.Exists(exe);
                       if (exists)
                           Console.WriteLine("\nFound installation: {0}", exe);
                       return exists;
                   }).Select(exe => {
                       Console.WriteLine("Getting skin information...");
                       return new UnitySkin(exe);
                   }).Where(unity => {
                       if (unity.AddressOfSkinFlags == -1)
                           Console.WriteLine("Invalid executable, skin bytes not found");
                       return unity.AddressOfSkinFlags != -1;
                   }).Where(unity => {
                       var shouldChange = (TO_ENABLE && unity.IsWhiteSkin) || (!TO_ENABLE && unity.IsDarkSkin);
                       if (!shouldChange)
                           Console.WriteLine("Skin already applied, ignoring");
                       return shouldChange;
                   });

                foreach (var unity in unitys)
                    try {
                        Console.Write("Applying {0} skin ", TO_ENABLE ? "dark" : "white");
                        unity.SetDarkSkinEnable(TO_ENABLE);
                        Console.WriteLine("- Success!");
                    }
                    catch (Exception e) {
                        Console.WriteLine("- Error: {0}", e.Message);
                    }
            }
            catch (Exception e) {
                Console.WriteLine("\nError");
                Console.WriteLine(e.Message);
            }
            finally {
                Console.WriteLine("\nFinished");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }

        }
    }
}