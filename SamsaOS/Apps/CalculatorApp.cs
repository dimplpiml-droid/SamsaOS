using Cosmos.System.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SamsaOS.Apps
{
    public static class CalculatorApp
    {
        private static string display = "0";
        private static double accumulator = 0;
        private static char? pendingOp = null;
        private static bool startNew = true;

        private const int BtnW = 58;
        private const int BtnH = 42;
        private const int BtnGap = 6;
        private const int TitleH = 32;
        private const int DisplayH = 44;

        private static readonly string[][] Buttons =
        {
            new[] { "C", "/", "*", "X" },
            new[] { "7", "8", "9", "-" },
            new[] { "4", "5", "6", "+" },
            new[] { "1", "2", "3", "=" },
            new[] { "0", ".", "=" }
        };

        // cached pens
        private static readonly Pen WindowPen = new Pen(Color.FromArgb(28, 32, 44));
        private static readonly Pen TitlePen = new Pen(Color.FromArgb(52, 73, 94));
        private static readonly Pen WhitePen = new Pen(Color.White);
        private static readonly Pen ClosePen = new Pen(Color.DarkRed);
        private static readonly Pen DispBgPen = new Pen(Color.Black);
        private static readonly Pen LimePen = new Pen(Color.Lime);
        private static readonly Pen BtnDefaultPen = new Pen(Color.FromArgb(70, 70, 90));
        private static readonly Pen BtnRedPen = new Pen(Color.FromArgb(192, 57, 43));
        private static readonly Pen BtnGreenPen = new Pen(Color.FromArgb(39, 174, 96));
        private static readonly Pen BtnBluePen = new Pen(Color.FromArgb(41, 128, 185));

        public static void Run()
        {
            Console.Clear();
            Console.WriteLine("--- SamsaOS Calculate ---");
            Console.WriteLine("Enter expression (e.g. 5 + 3 * 2):");
            Console.WriteLine("Type 'exit' to quit.");

            while (true)
            {
                Console.Write("\ncalc> ");
                string input = Console.ReadLine();
                if (input.ToLower() == "exit") break;

                try
                {
                    double result = Evaluate(input);
                    Console.WriteLine($"Result: {result}");
                }
                catch (Exception)
                {
                    Console.WriteLine("Error: Invalid expression.");
                }
            }
        }

        public static void ResetGui()
        {
            display = "0";
            accumulator = 0;
            pendingOp = null;
            startNew = true;
        }

        public static void RenderGui(Canvas canvas, int x, int y, int width, int height)
        {
            canvas.DrawFilledRectangle(WindowPen, x, y, width, height);
            canvas.DrawFilledRectangle(TitlePen, x, y, width, TitleH);
            canvas.DrawString("Calculator", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, x + 10, y + 8);

            int closeX = x + width - 58;
            canvas.DrawFilledRectangle(ClosePen, closeX, y + 4, 50, 24);
            canvas.DrawString("X", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, closeX + 20, y + 8);

            int dispY = y + TitleH + 8;
            canvas.DrawFilledRectangle(DispBgPen, x + 10, dispY, width - 20, DisplayH);
            string shown = display.Length > 14 ? display.Substring(display.Length - 14) : display;
            canvas.DrawString(shown, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, LimePen, x + 16, dispY + 14);

            int gridX = x + 10;
            int gridY = dispY + DisplayH + 10;

            for (int row = 0; row < Buttons.Length; row++)
            {
                for (int col = 0; col < Buttons[row].Length; col++)
                {
                    string label = Buttons[row][col];
                    GetButtonRect(row, col, gridX, gridY, out int bx, out int by, out int bw, out int bh);

                    Pen bgPen = BtnDefaultPen;
                    if (label == "C" || label == "X") bgPen = BtnRedPen;
                    else if (label == "=") bgPen = BtnGreenPen;
                    else if (label == "+" || label == "-" || label == "*" || label == "/") bgPen = BtnBluePen;

                    canvas.DrawFilledRectangle(bgPen, bx, by, bw, bh);
                    int tx = bx + (bw / 2) - 4;
                    canvas.DrawString(label, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, tx, by + 14);
                }
            }
        }

        private static void GetButtonRect(int row, int col, int gridX, int gridY, out int bx, out int by, out int bw, out int bh)
        {
            bh = BtnH;
            by = gridY + row * (BtnH + BtnGap);
            bw = BtnW;
            bx = gridX + col * (BtnW + BtnGap);

            if (row == 4)
            {
                if (col == 0)
                {
                    bx = gridX;
                    bw = BtnW * 2 + BtnGap;
                }
                else if (col == 1)
                {
                    bx = gridX + 2 * (BtnW + BtnGap);
                }
                else
                {
                    bx = gridX + 3 * (BtnW + BtnGap);
                }
            }
        }

        public static bool HandleClick(int mX, int mY, int x, int y, int width, int height, out bool requestClose)
        {
            requestClose = false;

            if (mX < x || mX > x + width || mY < y || mY > y + height)
            {
                return false;
            }

            int closeX = x + width - 58;
            if (mX >= closeX && mX <= closeX + 50 && mY >= y + 4 && mY <= y + 28)
            {
                requestClose = true;
                return true;
            }

            int gridX = x + 10;
            int gridY = y + TitleH + DisplayH + 18;

            for (int row = 0; row < Buttons.Length; row++)
            {
                for (int col = 0; col < Buttons[row].Length; col++)
                {
                    GetButtonRect(row, col, gridX, gridY, out int bx, out int by, out int bw, out int bh);

                    if (mX >= bx && mX <= bx + bw && mY >= by && mY <= by + bh)
                    {
                        PressButton(Buttons[row][col]);
                        return true;
                    }
                }
            }

            return true;
        }

        private static void PressButton(string key)
        {
            if (key == "X") return;

            if (key == "C")
            {
                ResetGui();
                return;
            }

            if (key == "=")
            {
                ComputePending();
                return;
            }

            if (key == "+" || key == "-" || key == "*" || key == "/")
            {
                ComputePending();
                pendingOp = key[0];
                startNew = true;
                return;
            }

            if (key == ".")
            {
                if (startNew)
                {
                    display = "0.";
                    startNew = false;
                }
                else if (!display.Contains("."))
                {
                    display += ".";
                }
                return;
            }

            if (startNew)
            {
                display = key;
                startNew = false;
            }
            else if (display == "0")
            {
                display = key;
            }
            else if (display.Length < 12)
            {
                display += key;
            }
        }

        private static void ComputePending()
        {
            try
            {
                double current = ParseDisplay();
                if (pendingOp == null)
                {
                    accumulator = current;
                }
                else
                {
                    accumulator = ApplyOp(accumulator, pendingOp.Value, current);
                }

                display = FormatNumber(accumulator);
                pendingOp = null;
                startNew = true;
            }
            catch
            {
                display = "Error";
                pendingOp = null;
                startNew = true;
            }
        }

        private static double ParseDisplay()
        {
            if (display == "Error") return 0;
            return double.Parse(display);
        }

        private static double ApplyOp(double a, char op, double b)
        {
            switch (op)
            {
                case '+': return a + b;
                case '-': return a - b;
                case '*': return a * b;
                case '/':
                    if (b == 0) throw new Exception("Division by zero.");
                    return a / b;
                default: return b;
            }
        }

        private static string FormatNumber(double v)
        {
            long whole = (long)v;
            if (v == whole) return whole.ToString();
            return v.ToString();
        }

        private static double Evaluate(string expression)
        {
            List<string> tokens = new List<string>(expression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            if (tokens.Count == 0 || tokens.Count % 2 == 0)
            {
                throw new Exception("Invalid expression.");
            }

            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i] == "*" || tokens[i] == "/")
                {
                    if (i == 0 || i >= tokens.Count - 1)
                    {
                        throw new Exception("Invalid expression.");
                    }
                    double left = double.Parse(tokens[i - 1]);
                    double right = double.Parse(tokens[i + 1]);
                    if (tokens[i] == "/" && right == 0)
                    {
                        throw new Exception("Division by zero.");
                    }
                    double res = (tokens[i] == "*") ? left * right : left / right;

                    tokens[i - 1] = res.ToString();
                    tokens.RemoveAt(i);
                    tokens.RemoveAt(i);
                    i--;
                }
            }

            double result = double.Parse(tokens[0]);
            for (int i = 1; i < tokens.Count; i += 2)
            {
                if (i + 1 >= tokens.Count)
                {
                    throw new Exception("Invalid expression.");
                }
                string op = tokens[i];
                double next = double.Parse(tokens[i + 1]);
                if (op == "+") result += next;
                else if (op == "-") result -= next;
            }

            return result;
        }
    }
}