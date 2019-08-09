using System;

namespace DarkSkin {
    public struct TempConsoleColor : IDisposable {

        private ConsoleColor oldFg;
        private ConsoleColor oldBg;

        public TempConsoleColor(ConsoleColor fg, ConsoleColor bg = ConsoleColor.Black) {
            oldFg = Console.ForegroundColor;
            oldBg = Console.BackgroundColor;
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        public void Dispose() {
            Console.ForegroundColor = oldFg;
            Console.BackgroundColor = oldBg;
        }

    }
}