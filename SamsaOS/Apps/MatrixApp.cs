using System;

namespace SamsaOS.Apps
{
    public static class MatrixApp
    {
        public static void Run()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;

            Random random = new Random();
            bool running = true;


            while (running)
            {
                // Если нажата любая клавиша - выходим
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    running = false;
                }


                for (int i = 0; i < 20; i++)
                {
                    int x = random.Next(0, 80);
                    int y = random.Next(0, 25);
                    char symbol = (char)random.Next(33, 126);

                    Console.SetCursorPosition(x, y);
                    Console.Write(symbol);
                }

                for (int delay = 0; delay < 1000000; delay++) { }
            }

            Console.ResetColor();
            Console.Clear();
        }
    }
}