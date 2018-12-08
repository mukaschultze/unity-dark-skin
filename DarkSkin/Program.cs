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

                Directory.EnumerateDirectories(Environment.CurrentDirectory, "*", SearchOption.AllDirectories)
                  .AsParallel()
                  .Select(dir => Path.Combine(dir, "Unity.exe"))
                  .Where(exe => File.Exists(exe))
                  .Select(exe => new UnitySkin(exe))
                  .Where(unity => unity.OffsetOfSkinFlags != -1 && unity.SkinIndex != -1)
                  .Where(unity => {
                      var shouldChange = (TO_ENABLE && unity.IsWhiteSkin) || (!TO_ENABLE && unity.IsDarkSkin);
                      if(!shouldChange)
                          unity.Log("Skin already applied, ignoring");
                      return shouldChange;
                  })
                  .ForAll(unity => unity.SetDarkSkinEnable(TO_ENABLE));

            }
            catch(Exception e) {
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