using System;
using System.Collections.Generic;
using System.Threading;

namespace SamsaOS.Apps
{
    public static class SnakeGame
    {
        private const int Width = 30;
        private const int Height = 15;
        private const int OffsetX = 5;
        private const int OffsetY = 2;


        private struct Point
        {
            public int X;
            public int Y;
            public Point(int x, int y) { X = x; Y = y; }
        }

        private static List<Point> snake = new List<Point>();
        private static Point food;
        private static Point dir = new Point(1, 0);
        private static bool running = true;
        private static int score = 0;
        private static Random rng = new Random();

        public static void Run()
        {
            Initialize();
            while (running)
            {
                HandleInput();
                Update();
                Draw();

                Thread.Sleep(100);
            }
            EndGame();
        }

        private static void Initialize()
        {
            Console.Clear();


            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (y == 0 || y == Height - 1 || x == 0 || x == Width - 1)
                        SetChar(x, y, '#');

            snake.Clear();
            snake.Add(new Point(Width / 2, Height / 2));
            dir = new Point(1, 0);
            score = 0;
            running = true;
            SpawnFood();
        }

        private static void SpawnFood()
        {
            bool valid;
            do
            {
                food = new Point(rng.Next(1, Width - 1), rng.Next(1, Height - 1));
                valid = true;
                foreach (var seg in snake)
                {
                    if (seg.X == food.X && seg.Y == food.Y)
                    {
                        valid = false;
                        break;
                    }
                }
            } while (!valid);
        }

        private static void HandleInput()
        {
            try
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                            if (dir.Y != 1) dir = new Point(0, -1);
                            break;
                        case ConsoleKey.DownArrow:
                            if (dir.Y != -1) dir = new Point(0, 1);
                            break;
                        case ConsoleKey.LeftArrow:
                            if (dir.X != 1) dir = new Point(-1, 0);
                            break;
                        case ConsoleKey.RightArrow:
                            if (dir.X != -1) dir = new Point(1, 0);
                            break;
                    }
                }
            }
            catch {  }
        }

        private static void Update()
        {
            var head = snake[0];
            var newHead = new Point(head.X + dir.X, head.Y + dir.Y);

            // Столкновение со стеной
            if (newHead.X <= 0 || newHead.X >= Width - 1 ||
                newHead.Y <= 0 || newHead.Y >= Height - 1)
            {
                running = false;
                return;
            }

            // Столкновение с собой 
            foreach (var seg in snake)
            {
                if (seg.X == newHead.X && seg.Y == newHead.Y)
                {
                    running = false;
                    return;
                }
            }

            snake.Insert(0, newHead);

            // Съели еду?
            if (newHead.X == food.X && newHead.Y == food.Y)
            {
                score += 10;
                SpawnFood();
            }
            else
            {
                snake.RemoveAt(snake.Count - 1);
            }
        }

        private static void Draw()
        {
            // Очищаем только игровую область
            for (int y = 1; y < Height - 1; y++)
                for (int x = 1; x < Width - 1; x++)
                    SetChar(x, y, ' ');

            // Рисуем тело
            foreach (var seg in snake)
                SetChar(seg.X, seg.Y, 'O');

            // Рисуем голову
            SetChar(snake[0].X, snake[0].Y, '@');

            // Рисуем еду
            SetChar(food.X, food.Y, '*');

            // Вывод счёта
            Console.SetCursorPosition(OffsetX, OffsetY + Height + 1);
            Console.Write("Score: " + score + "        ");
        }

        private static void SetChar(int x, int y, char c)
        {
            Console.SetCursorPosition(OffsetX + x, OffsetY + y);
            Console.Write(c);
        }

        private static void EndGame()
        {
            Console.SetCursorPosition(OffsetX, OffsetY + Height + 2);
            Console.WriteLine("GAME OVER! Final Score: " + score);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}