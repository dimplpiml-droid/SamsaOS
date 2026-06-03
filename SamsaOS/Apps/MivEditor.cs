using Cosmos.System.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace SamsaOS.Apps
{
    public static class MivEditor
    {
        private static List<string> lines = new List<string>();
        private static int cursorX = 0;
        private static int cursorY = 0;
        private static string filePath;

        private static int guiViewTopLine = 0;
        private static int guiViewLeftChar = 0;

        private const int GuiFontWidth = 8;
        private const int GuiLineHeight = 18;
        private const int GuiPadding = 18;
        private const int GuiVisibleChars = 68;
        private const int GuiVisibleLines = 20;
        private const int ConsoleVisibleLines = 22;
        private const int StringBuilderLineThreshold = 64;

        private static bool textDirty = true;
        private static int cacheTop = -1;
        private static int cacheLeft = -1;
        private static readonly string[] visibleLines = new string[GuiVisibleLines];

        private static int consoleViewTop = 0;

        private static readonly Pen PenWindow = new Pen(Color.FromArgb(28, 32, 44));
        private static readonly Pen PenTitle = new Pen(Color.FromArgb(52, 73, 94));
        private static readonly Pen PenWhite = new Pen(Color.White);
        private static readonly Pen PenYellow = new Pen(Color.Yellow);
        private static readonly Pen PenGreen = new Pen(Color.DarkGreen);
        private static readonly Pen PenRed = new Pen(Color.DarkRed);
        private static readonly Pen PenBlack = new Pen(Color.Black);
        private static readonly Pen PenGray = new Pen(Color.Gray);
        private static readonly Pen PenLightGray = new Pen(Color.LightGray);

        public static void Run(string filename)
        {
            OpenFile(@"0:\" + filename);

            while (true)
            {
                RenderConsole();
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.S && key.Modifiers == ConsoleModifiers.Control)
                {
                    Save();
                }
                else if (key.Key == ConsoleKey.F2)
                {
                    SaveAndClose();
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

        public static void OpenFile(string path)
        {
            filePath = path;
            LoadFile();
            cursorX = 0;
            cursorY = 0;
            guiViewTopLine = 0;
            guiViewLeftChar = 0;
            consoleViewTop = 0;
            MarkTextDirty();
        }

        public static void HandleGuiKey(ConsoleKeyInfo key)
        {
            HandleInput(key);
            EnsureGuiCursorVisible();
        }

        public static void RenderGui(Canvas canvas, int x, int y, int width, int height)
        {
            EnsureGuiCursorVisible();
            RefreshVisibleCacheIfNeeded();

            int titleBarHeight = 32;
            int headerY = y + titleBarHeight + 2;
            int textAreaX = x + GuiPadding;
            int textAreaY = headerY + 18;
            int textAreaW = width - (GuiPadding * 2);
            int textAreaH = height - titleBarHeight - 64;
            int saveButtonX = x + width - 170;
            int closeButtonX = x + width - 88;

            canvas.DrawFilledRectangle(PenWindow, x, y, width, height);
            canvas.DrawFilledRectangle(PenTitle, x, y, width, titleBarHeight);
            canvas.DrawString("Miv Editor", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, PenWhite, x + 12, y + 8);

            canvas.DrawFilledRectangle(PenGreen, saveButtonX, y + 4, 70, 24);
            canvas.DrawString("Save", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, PenWhite, saveButtonX + 16, y + 8);

            canvas.DrawFilledRectangle(PenRed, closeButtonX, y + 4, 70, 24);
            canvas.DrawString("Close", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, PenWhite, closeButtonX + 11, y + 8);

            canvas.DrawString(GetDisplayName(), Cosmos.System.Graphics.Fonts.PCScreenFont.Default, PenYellow, x + 12, headerY);
            canvas.DrawString("Ctrl+S save   F2 save+close   Esc close", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, PenWhite, x + 250, headerY);

            canvas.DrawFilledRectangle(PenBlack, textAreaX, textAreaY, textAreaW, textAreaH);
            canvas.DrawFilledRectangle(PenGray, textAreaX, textAreaY, textAreaW, 1);
            canvas.DrawFilledRectangle(PenGray, textAreaX, textAreaY + textAreaH - 1, textAreaW, 1);
            canvas.DrawFilledRectangle(PenGray, textAreaX, textAreaY, 1, textAreaH);
            canvas.DrawFilledRectangle(PenGray, textAreaX + textAreaW - 1, textAreaY, 1, textAreaH);

            int endLine = Math.Min(lines.Count, guiViewTopLine + GuiVisibleLines);
            for (int i = guiViewTopLine; i < endLine; i++)
            {
                int row = i - guiViewTopLine;
                canvas.DrawString(visibleLines[row], Cosmos.System.Graphics.Fonts.PCScreenFont.Default, PenWhite, textAreaX + 4, textAreaY + 4 + (row * GuiLineHeight));
            }

            if (cursorY >= guiViewTopLine && cursorY < guiViewTopLine + GuiVisibleLines)
            {
                int caretScreenX = textAreaX + 4 + ((cursorX - guiViewLeftChar) * GuiFontWidth);
                int caretScreenY = textAreaY + 4 + ((cursorY - guiViewTopLine) * GuiLineHeight);
                canvas.DrawFilledRectangle(PenWhite, caretScreenX, caretScreenY + 2, 2, GuiLineHeight - 2);
            }

            canvas.DrawString($"Line {cursorY + 1}, Col {cursorX + 1}", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, PenLightGray, x + 12, y + height - 22);
        }

        public static void Save()
        {
            SaveInternal(true);
        }

        public static void SaveSilently()
        {
            SaveInternal(false);
        }

        public static void SaveAndClose()
        {
            SaveInternal(false);
        }

        private static void LoadFile()
        {
            if (File.Exists(filePath))
            {
                lines = new List<string>(File.ReadAllLines(filePath));
            }
            else
            {
                lines = new List<string> { "" };
            }
            MarkTextDirty();
        }

        private static void SaveInternal(bool showPrompt)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            using (StreamWriter sw = new StreamWriter(filePath))
            {
                foreach (var line in lines)
                {
                    sw.WriteLine(line);
                }
            }

            if (showPrompt)
            {
                Console.SetCursorPosition(0, ConsoleVisibleLines + 2);
                Console.Write("Saved! Press any key to continue...");
                Console.ReadKey(true);
            }
        }

        private static void RenderConsole()
        {
            EnsureConsoleViewVisible();
            Console.Clear();

            int end = Math.Min(consoleViewTop + ConsoleVisibleLines, lines.Count);
            for (int i = consoleViewTop; i < end; i++)
            {
                Console.WriteLine(lines[i]);
            }

            NormalizeCursor();
            int screenY = cursorY - consoleViewTop;
            if (screenY >= 0 && screenY < ConsoleVisibleLines)
            {
                Console.SetCursorPosition(cursorX, screenY);
            }
        }

        private static void EnsureConsoleViewVisible()
        {
            if (cursorY < consoleViewTop)
            {
                consoleViewTop = cursorY;
            }
            else if (cursorY >= consoleViewTop + ConsoleVisibleLines)
            {
                consoleViewTop = cursorY - ConsoleVisibleLines + 1;
            }
        }

        private static void HandleInput(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (cursorY > 0) cursorY--;
                    break;
                case ConsoleKey.DownArrow:
                    if (cursorY < lines.Count - 1) cursorY++;
                    break;
                case ConsoleKey.LeftArrow:
                    if (cursorX > 0) cursorX--;
                    break;
                case ConsoleKey.RightArrow:
                    if (cursorX < lines[cursorY].Length) cursorX++;
                    break;
                case ConsoleKey.Backspace:
                    if (cursorX > 0)
                    {
                        lines[cursorY] = RemoveCharAt(lines[cursorY], cursorX - 1);
                        cursorX--;
                    }
                    else if (cursorY > 0)
                    {
                        string oldLine = lines[cursorY];
                        cursorX = lines[cursorY - 1].Length;
                        lines[cursorY - 1] = lines[cursorY - 1] + oldLine;
                        lines.RemoveAt(cursorY);
                        cursorY--;
                    }
                    break;
                case ConsoleKey.Enter:
                    string partAfter = lines[cursorY].Substring(cursorX);
                    lines[cursorY] = lines[cursorY].Substring(0, cursorX);
                    lines.Insert(cursorY + 1, partAfter);
                    cursorY++;
                    cursorX = 0;
                    break;
                case ConsoleKey.Tab:
                    lines[cursorY] = InsertTextAt(lines[cursorY], cursorX, "    ");
                    cursorX += 4;
                    break;
                case ConsoleKey.Home:
                    cursorX = 0;
                    break;
                case ConsoleKey.End:
                    cursorX = lines[cursorY].Length;
                    break;
                default:
                    if (key.KeyChar >= 32 && key.KeyChar <= 126)
                    {
                        lines[cursorY] = InsertTextAt(lines[cursorY], cursorX, key.KeyChar.ToString());
                        cursorX++;
                    }
                    break;
            }

            NormalizeCursor();
            MarkTextDirty();
        }

        private static string InsertTextAt(string line, int index, string text)
        {
            if (line.Length < StringBuilderLineThreshold)
            {
                return line.Insert(index, text);
            }

            StringBuilder sb = new StringBuilder(line);
            sb.Insert(index, text);
            return sb.ToString();
        }

        private static string RemoveCharAt(string line, int index)
        {
            if (line.Length < StringBuilderLineThreshold)
            {
                return line.Remove(index, 1);
            }

            StringBuilder sb = new StringBuilder(line);
            sb.Remove(index, 1);
            return sb.ToString();
        }

        private static void NormalizeCursor()
        {
            if (lines.Count == 0)
            {
                lines.Add(string.Empty);
            }

            if (cursorY < 0) cursorY = 0;
            if (cursorY >= lines.Count) cursorY = lines.Count - 1;
            if (cursorX < 0) cursorX = 0;
            if (cursorX > lines[cursorY].Length) cursorX = lines[cursorY].Length;
        }

        private static void EnsureGuiCursorVisible()
        {
            NormalizeCursor();

            int oldTop = guiViewTopLine;
            int oldLeft = guiViewLeftChar;

            if (cursorY < guiViewTopLine)
            {
                guiViewTopLine = cursorY;
            }
            else if (cursorY >= guiViewTopLine + GuiVisibleLines)
            {
                guiViewTopLine = cursorY - GuiVisibleLines + 1;
            }

            if (cursorX < guiViewLeftChar)
            {
                guiViewLeftChar = cursorX;
            }
            else if (cursorX >= guiViewLeftChar + GuiVisibleChars)
            {
                guiViewLeftChar = cursorX - GuiVisibleChars + 1;
            }

            if (guiViewTopLine != oldTop || guiViewLeftChar != oldLeft)
            {
                MarkTextDirty();
            }
        }

        private static void MarkTextDirty()
        {
            textDirty = true;
        }

        private static void RefreshVisibleCacheIfNeeded()
        {
            if (!textDirty && cacheTop == guiViewTopLine && cacheLeft == guiViewLeftChar)
            {
                return;
            }

            int endLine = Math.Min(lines.Count, guiViewTopLine + GuiVisibleLines);
            int row = 0;
            for (int i = guiViewTopLine; i < endLine; i++, row++)
            {
                string line = lines[i] ?? string.Empty;
                visibleLines[row] = GetVisibleLine(line);
            }

            for (; row < GuiVisibleLines; row++)
            {
                visibleLines[row] = string.Empty;
            }

            cacheTop = guiViewTopLine;
            cacheLeft = guiViewLeftChar;
            textDirty = false;
        }

        private static string GetVisibleLine(string line)
        {
            if (string.IsNullOrEmpty(line) || guiViewLeftChar >= line.Length)
            {
                return string.Empty;
            }

            int visibleLength = Math.Min(GuiVisibleChars, line.Length - guiViewLeftChar);
            return line.Substring(guiViewLeftChar, visibleLength);
        }

        private static string GetDisplayName()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return "Untitled";
            }

            return Path.GetFileName(filePath);
        }
    }
}