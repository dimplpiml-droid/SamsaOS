using Cosmos.System.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace SamsaOS.Apps
{
    public static class SnakeGame
    {
        private const int Width = 30;
        private const int Height = 15;
        private const int OffsetX = 5;
        private const int OffsetY = 2;
        private const int GuiCell = 12;
        private const int GuiMoveDelay = 7;

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

        private static bool guiMode = false;
        private static bool closeRequested = false;
        private static int guiTick;
        private static bool borderDrawn;
        private static Point? cellToClear;

        private static readonly Pen BoardBgPen = new Pen(Color.FromArgb(35, 50, 35));
        private static readonly Pen WindowBgPen = new Pen(Color.FromArgb(20, 28, 20));
        private static readonly Pen TitleBarPen = new Pen(Color.FromArgb(39, 174, 96));
        private static readonly Pen CloseBtnPen = new Pen(Color.DarkRed);
        private static readonly Pen BoardBorderPen = new Pen(Color.FromArgb(30, 40, 30));
        private static readonly Pen FoodPen = new Pen(Color.Red);
        private static readonly Pen HeadPen = new Pen(Color.Lime);
        private static readonly Pen BodyPen = new Pen(Color.FromArgb(46, 204, 113));
        private static readonly Pen PadBtnPen = new Pen(Color.FromArgb(52, 73, 94));
        private static readonly Pen GameOverBgPen = new Pen(Color.FromArgb(15, 20, 15));
        private static readonly Pen RetryBtnPen = new Pen(Color.DarkGreen);
        private static readonly Pen WhitePen = new Pen(Color.White);
        private static readonly Pen LightGrayPen = new Pen(Color.LightGray);
        private static readonly Pen RedPen = new Pen(Color.Red);

        public static bool IsRunning => running;
        public static bool CloseRequested => closeRequested;

        public static void ClearCloseRequest()
        {
            closeRequested = false;
        }

        public static void Run()
        {
            guiMode = false;
            Initialize();
            borderDrawn = false;
            while (running)
            {
                HandleConsoleInput();
                Update();
                DrawConsole();
                Thread.Sleep(80);
            }
            EndGameConsole();
        }

        public static void ResetGui()
        {
            guiMode = true;
            closeRequested = false;
            guiTick = 0;
            borderDrawn = false;
            cellToClear = null;
            Initialize();
        }

        public static void Tick()
        {
            if (!guiMode || !running) return;

            guiTick++;
            if (guiTick < GuiMoveDelay) return;
            guiTick = 0;
            Update();
        }

        public static void HandleGuiKey(ConsoleKeyInfo key)
        {
            if (!guiMode) return;

            if (key.Key == ConsoleKey.Escape)
            {
                closeRequested = true;
                return;
            }

            if (!running && key.Key == ConsoleKey.Enter)
            {
                Initialize();
                return;
            }

            ApplyDirectionKey(key.Key);
        }

        public static bool HandleClick(int mX, int mY, int x, int y, int width, int height, out bool requestClose)
        {
            requestClose = false;
            if (mX < x || mX > x + width || mY < y || mY > y + height) return false;

            int closeX = x + width - 58;
            if (mX >= closeX && mX <= closeX + 50 && mY >= y + 4 && mY <= y + 28)
            {
                requestClose = true;
                running = false;
                return true;
            }

            if (!running)
            {
                int retryX = x + width / 2 - 40;
                if (mX >= retryX && mX <= retryX + 80 && mY >= y + height - 50 && mY <= y + height - 26)
                {
                    Initialize();
                }
                return true;
            }

            int padX = x + 15;
            int padY = y + height - 95;
            int b = 44;
            int g = 6;

            if (Hit(mX, mY, padX + b + g, padY, b, b)) ApplyDirectionKey(ConsoleKey.UpArrow);
            else if (Hit(mX, mY, padX + b + g, padY + b + g, b, b)) ApplyDirectionKey(ConsoleKey.DownArrow);
            else if (Hit(mX, mY, padX, padY + b + g, b, b)) ApplyDirectionKey(ConsoleKey.LeftArrow);
            else if (Hit(mX, mY, padX + (b + g) * 2, padY + b + g, b, b)) ApplyDirectionKey(ConsoleKey.RightArrow);

            return true;
        }

        public static void RenderGui(Canvas canvas, int x, int y, int width, int height)
        {
            canvas.DrawFilledRectangle(WindowBgPen, x, y, width, height);
            canvas.DrawFilledRectangle(TitleBarPen, x, y, width, 32);
            canvas.DrawString("Snake", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, x + 12, y + 8);

            int closeX = x + width - 58;
            canvas.DrawFilledRectangle(CloseBtnPen, closeX, y + 4, 50, 24);
            canvas.DrawString("X", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, closeX + 20, y + 8);

            int boardX = x + 15;
            int boardY = y + 42;
            int boardW = Width * GuiCell;
            int boardH = Height * GuiCell;

            canvas.DrawFilledRectangle(BoardBorderPen, boardX - 2, boardY - 2, boardW + 4, boardH + 4);

            // Solid board (no per-cell or grid lines) - huge perf win vs original 450 rects or 47 lines
            canvas.DrawFilledRectangle(BoardBgPen, boardX, boardY, boardW, boardH);

            canvas.DrawFilledRectangle(FoodPen, boardX + food.X * GuiCell + 2, boardY + food.Y * GuiCell + 2, GuiCell - 4, GuiCell - 4);

            for (int i = 0; i < snake.Count; i++)
            {
                Pen segPen = (i == 0) ? HeadPen : BodyPen;
                int px = boardX + snake[i].X * GuiCell + 1;
                int py = boardY + snake[i].Y * GuiCell + 1;
                canvas.DrawFilledRectangle(segPen, px, py, GuiCell - 2, GuiCell - 2);
            }

            canvas.DrawString("Score: " + score, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, x + 12, y + height - 118);
            canvas.DrawString("Arrows / pad   Esc close", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, LightGrayPen, x + 120, y + height - 118);

            DrawPad(canvas, x + 15, y + height - 95);

            if (!running)
            {
                canvas.DrawFilledRectangle(GameOverBgPen, boardX, boardY, boardW, boardH);
                canvas.DrawString("GAME OVER", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, RedPen, x + width / 2 - 40, y + height / 2 - 20);
                canvas.DrawFilledRectangle(RetryBtnPen, x + width / 2 - 40, y + height - 50, 80, 24);
                canvas.DrawString("Retry", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, x + width / 2 - 20, y + height - 46);
            }
        }

        private static void DrawPad(Canvas canvas, int padX, int padY)
        {
            int b = 44;
            int g = 6;
            canvas.DrawFilledRectangle(PadBtnPen, padX + b + g, padY, b, b);
            canvas.DrawString("^", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, padX + b + g + 18, padY + 14);
            canvas.DrawFilledRectangle(PadBtnPen, padX + b + g, padY + b + g, b, b);
            canvas.DrawString("v", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, padX + b + g + 18, padY + b + g + 14);
            canvas.DrawFilledRectangle(PadBtnPen, padX, padY + b + g, b, b);
            canvas.DrawString("<", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, padX + 16, padY + b + g + 14);
            canvas.DrawFilledRectangle(PadBtnPen, padX + (b + g) * 2, padY + b + g, b, b);
            canvas.DrawString(">", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, padX + (b + g) * 2 + 16, padY + b + g + 14);
        }

        private static bool Hit(int mX, int mY, int x, int y, int w, int h)
        {
            return mX >= x && mX <= x + w && mY >= y && mY <= y + h;
        }

        private static void Initialize()
        {
            snake.Clear();
            snake.Add(new Point(Width / 2, Height / 2));
            dir = new Point(1, 0);
            score = 0;
            running = true;
            cellToClear = null;
            SpawnFood();

            if (!guiMode)
            {
                Console.Clear();
                borderDrawn = false;
            }
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

        private static void HandleConsoleInput()
        {
            try
            {
                while (Console.KeyAvailable)
                {
                    ApplyDirectionKey(Console.ReadKey(true).Key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Snake Input Error]: {ex.Message}");
            }
        }

        private static void ApplyDirectionKey(ConsoleKey key)
        {
            if (!running) return;

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

        private static void Update()
        {
            var head = snake[0];
            var newHead = new Point(head.X + dir.X, head.Y + dir.Y);

            if (newHead.X <= 0 || newHead.X >= Width - 1 || newHead.Y <= 0 || newHead.Y >= Height - 1)
            {
                running = false;
                return;
            }

            bool willEat = newHead.X == food.X && newHead.Y == food.Y;
            int segmentsToCheck = willEat ? snake.Count : snake.Count - 1;
            for (int i = 0; i < segmentsToCheck; i++)
            {
                if (snake[i].X == newHead.X && snake[i].Y == newHead.Y)
                {
                    running = false;
                    return;
                }
            }

            if (!willEat)
            {
                cellToClear = snake[snake.Count - 1];
            }
            else
            {
                cellToClear = null;
            }

            snake.Insert(0, newHead);

            if (willEat)
            {
                score += 10;
                SpawnFood();
            }
            else
            {
                snake.RemoveAt(snake.Count - 1);
            }
        }

        private static void DrawConsole()
        {
            if (!borderDrawn)
            {
                for (int gy = 0; gy < Height; gy++)
                    for (int gx = 0; gx < Width; gx++)
                        if (gy == 0 || gy == Height - 1 || gx == 0 || gx == Width - 1)
                            SetChar(gx, gy, '#');
                borderDrawn = true;
            }

            if (cellToClear.HasValue)
            {
                var c = cellToClear.Value;
                SetChar(c.X, c.Y, ' ');
            }

            if (snake.Count > 1)
                SetChar(snake[1].X, snake[1].Y, 'O');

            SetChar(snake[0].X, snake[0].Y, '@');
            SetChar(food.X, food.Y, '*');

            Console.SetCursorPosition(OffsetX, OffsetY + Height + 1);
            Console.Write("Score: " + score + "   ");
        }

        private static void SetChar(int x, int y, char c)
        {
            Console.SetCursorPosition(OffsetX + x, OffsetY + y);
            Console.Write(c);
        }

        private static void EndGameConsole()
        {
            Console.SetCursorPosition(OffsetX, OffsetY + Height + 2);
            Console.WriteLine("GAME OVER! Final Score: " + score);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}