using System;
using System.Collections.Generic;
using System.IO;

namespace SamsaOS.Apps
{
    public static class MivEditor
    {
        private static List<string> lines = new List<string>();
        private static int cursorX = 0;
        private static int cursorY = 0;
        private static string filePath;

        public static void Run(string filename)
        {
            filePath = @"0:\" + filename;

            // Загрузка
            if (File.Exists(filePath))
                lines = new List<string>(File.ReadAllLines(filePath));
            else
                lines = new List<string> { "" };

            cursorX = 0;
            cursorY = 0;

            while (true)
            {
                Render();
                var key = Console.ReadKey(true);


                if (key.Key == ConsoleKey.S && key.Modifiers == ConsoleModifiers.Control)
                {
                    Save(false); 
                }

                else if (key.Key == ConsoleKey.F2)
                {
                    Save(true);
                    break;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }
                else
                {
                    HandleInput(key);
                }
            }
            Console.Clear();
        }

        private static void Save(bool shouldExit)
        {

            if (File.Exists(filePath)) File.Delete(filePath);

            using (StreamWriter sw = new StreamWriter(filePath))
            {
                foreach (var line in lines)
                {
                    sw.WriteLine(line);
                }
            }


            if (!shouldExit)
            {
                Console.SetCursorPosition(0, lines.Count + 1);
                Console.Write("Saved! Press any key to continue...");
                Console.ReadKey(true);
            }
        }

        private static void Render()
        {
            Console.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                Console.WriteLine(lines[i]);
            }
            // Ограничители курсора
            if (cursorY >= lines.Count) cursorY = lines.Count - 1;
            if (cursorX > lines[cursorY].Length) cursorX = lines[cursorY].Length;

            Console.SetCursorPosition(cursorX, cursorY);
        }

        private static void HandleInput(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow: if (cursorY > 0) cursorY--; break;
                case ConsoleKey.DownArrow: if (cursorY < lines.Count - 1) cursorY++; break;
                case ConsoleKey.LeftArrow: if (cursorX > 0) cursorX--; break;
                case ConsoleKey.RightArrow: if (cursorX < lines[cursorY].Length) cursorX++; break;
                case ConsoleKey.Backspace:
                    if (cursorX > 0)
                    {
                        lines[cursorY] = lines[cursorY].Remove(cursorX - 1, 1);
                        cursorX--;
                    }
                    else if (cursorY > 0)
                    {
                        string oldLine = lines[cursorY];
                        cursorX = lines[cursorY - 1].Length;
                        lines[cursorY - 1] += oldLine;
                        lines.RemoveAt(cursorY);
                        cursorY--;
                    }
                    break;
                case ConsoleKey.Enter:
                    string partAfter = lines[cursorY].Substring(cursorX);
                    lines[cursorY] = lines[cursorY].Substring(0, cursorX);
                    lines.Insert(cursorY + 1, partAfter);
                    cursorY++; cursorX = 0;
                    break;
                default:
                    if (key.KeyChar >= 32 && key.KeyChar <= 126)
                    {
                        lines[cursorY] = lines[cursorY].Insert(cursorX, key.KeyChar.ToString());
                        cursorX++;
                    }
                    break;
            }
        }
    }
}