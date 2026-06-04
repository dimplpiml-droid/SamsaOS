using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using Cosmos.System;
using Cosmos.System.Graphics;

namespace SamsaOS.GUI
{
    public static class NotepadGUI
    {
        public static bool isActive = false;
        public static bool isFullscreen = false;
        private static bool isClosing = false;

        // Позиция и размеры
        private static int windowX = 50, currentY = 600, width = 500, height = 350;
        private static int targetX = 50, targetY = 50, targetWidth = 500, targetHeight = 350;

        // Текст и курсор
        private static List<string> lines = new List<string> { "" };
        private static int cursorX = 0, cursorY = 0;
        private static bool showCursor = true;
        private static int cursorTimer = 0;

        public static string currentFilePath = @"0:\note.txt";
        private static bool isMousePressed = false;

        // ===== ПЕРЕМЕННЫЕ ДЛЯ ОКНА СОХРАНЕНИЯ =====
        private static bool isSaveDialogOpen = false;
        private static string inputSavePath = @"0:\";
        private static int saveCursorX = 3; // Отдельный курсор для окна сохранения (начинается после "0:\")

        public static void Open(string path = @"0:\note.txt")
        {
            currentFilePath = path;
            isClosing = false;
            isSaveDialogOpen = false;
            lines = new List<string>();

            if (File.Exists(currentFilePath))
            {
                lines.AddRange(File.ReadAllText(currentFilePath).Split('\n'));
            }
            if (lines.Count == 0) lines.Add("");

            isActive = true;
            currentY = 600; // Стартуем снизу

            cursorX = 0;
            cursorY = 0;
            SetNormalMode();
        }

        private static void SetFullscreenMode()
        {
            targetX = 0;
            targetY = 0;
            targetWidth = 800;
            targetHeight = 530;
            isFullscreen = true;
        }

        private static void SetNormalMode()
        {
            targetX = 50;
            targetY = 50;
            targetWidth = 500;
            targetHeight = 350;
            isFullscreen = false;
        }

        private static int SmoothStep(int current, int target)
        {
            if (current == target) return current;
            int diff = target - current;
            int step = diff / 4;
            if (step == 0) step = diff > 0 ? 1 : -1;
            return current + step;
        }

        public static void Render(Canvas canvas)
        {
            if (!isActive) return;

            windowX = SmoothStep(windowX, targetX);
            currentY = SmoothStep(currentY, targetY);
            width = SmoothStep(width, targetWidth);
            height = SmoothStep(height, targetHeight);

            if (isClosing && currentY >= 590)
            {
                isActive = false;
                isClosing = false;
                return;
            }

            // Отрисовка основного окна
            if (!isFullscreen)
            {
                canvas.DrawFilledRectangle(new Pen(Color.FromArgb(20, 20, 20)), windowX + 5, currentY + 5, width, height);
            }

            canvas.DrawFilledRectangle(new Pen(Color.White), windowX, currentY, width, height);
            canvas.DrawFilledRectangle(new Pen(Color.FromArgb(45, 45, 48)), windowX, currentY, width, 30);

            canvas.DrawString($"Samsa Notepad - {currentFilePath}", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), windowX + 10, currentY + 8);

            canvas.DrawFilledRectangle(new Pen(Color.SeaGreen), windowX + width - 170, currentY, 80, 30);
            canvas.DrawString("SAVE", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), windowX + width - 150, currentY + 8);

            canvas.DrawFilledRectangle(new Pen(Color.Goldenrod), windowX + width - 80, currentY, 40, 30);
            string maxMinIcon = isFullscreen ? "[-]" : "[+]";
            canvas.DrawString(maxMinIcon, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), windowX + width - 70, currentY + 8);

            canvas.DrawFilledRectangle(new Pen(Color.DarkRed), windowX + width - 40, currentY, 40, 30);
            canvas.DrawString("X", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), windowX + width - 25, currentY + 8);

            // Отрисовка текста (с защитой от выхода за нижний край)
            int maxLines = (height - 60) / 20;
            for (int i = 0; i < Math.Min(lines.Count, maxLines); i++)
            {
                canvas.DrawString(lines[i], Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.Black), windowX + 10, currentY + 40 + (i * 20));
            }

            // Мигающий курсор (рисуем только если нет окна сохранения)
            cursorTimer++;
            if (cursorTimer > 30) { showCursor = !showCursor; cursorTimer = 0; }

            if (!isSaveDialogOpen)
            {
                if (showCursor && cursorY < maxLines)
                {
                    canvas.DrawFilledRectangle(new Pen(Color.Black), windowX + 10 + (cursorX * 8), currentY + 40 + (cursorY * 20), 2, 16);
                }
            }

            // ===== ОТРИСОВКА ОКНА "СОХРАНИТЬ КАК" =====
            if (isSaveDialogOpen)
            {
                int dialogWidth = 300;
                int dialogHeight = 100;
                int dX = windowX + (width / 2) - (dialogWidth / 2);
                int dY = currentY + (height / 2) - (dialogHeight / 2);

                // Тень и фон диалога
                canvas.DrawFilledRectangle(new Pen(Color.FromArgb(20, 20, 20)), dX + 5, dY + 5, dialogWidth, dialogHeight);
                canvas.DrawFilledRectangle(new Pen(Color.LightGray), dX, dY, dialogWidth, dialogHeight);
                canvas.DrawFilledRectangle(new Pen(Color.FromArgb(52, 152, 219)), dX, dY, dialogWidth, 25); // Синяя шапка

                canvas.DrawString("Save As...", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), dX + 10, dY + 5);
                canvas.DrawString("Path:", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.Black), dX + 10, dY + 40);

                // Поле ввода
                canvas.DrawFilledRectangle(new Pen(Color.White), dX + 50, dY + 35, 240, 20);

                // Рисуем сам текст пути
                canvas.DrawString(inputSavePath, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.Black), dX + 55, dY + 38);

                // Отрисовка мигающего курсора в окне сохранения
                if (showCursor)
                {
                    canvas.DrawFilledRectangle(new Pen(Color.Black), dX + 55 + (saveCursorX * 8), dY + 38, 2, 14);
                }

                canvas.DrawString("ENTER to Save  |  ESC to Cancel", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.DarkBlue), dX + 25, dY + 70);
            }

            HandleInput();
        }

        private static void HandleInput()
        {
            int mX = (int)MouseManager.X;
            int mY = (int)MouseManager.Y;

            // ===== ЛОГИКА ДИАЛОГА СОХРАНЕНИЯ (ПЕРЕХВАТЫВАЕТ ВЕСЬ ВВОД) =====
            if (isSaveDialogOpen)
            {
                if (KeyboardManager.TryReadKey(out KeyEvent saveKey))
                {
                    showCursor = true; // Сбрасываем мигание при печати
                    cursorTimer = 0;

                    if (saveKey.Key == ConsoleKeyEx.Escape)
                    {
                        isSaveDialogOpen = false; // Отмена
                    }
                    else if (saveKey.Key == ConsoleKeyEx.Enter)
                    {
                        // Пытаемся сохранить по новому пути
                        try
                        {
                            if (File.Exists(inputSavePath)) File.Delete(inputSavePath);
                            File.WriteAllLines(inputSavePath, lines.ToArray());
                            currentFilePath = inputSavePath; // Обновляем текущий путь в шапке
                        }
                        catch { }
                        isSaveDialogOpen = false;
                    }
                    else if (saveKey.Key == ConsoleKeyEx.Backspace)
                    {
                        // Удаляем символ ТОЛЬКО если курсор стоит правее "0:\" (индекс 3)
                        if (saveCursorX > 3)
                        {
                            inputSavePath = inputSavePath.Remove(saveCursorX - 1, 1);
                            saveCursorX--;
                        }
                    }
                    else if (saveKey.Key == ConsoleKeyEx.LeftArrow)
                    {
                        // Не пускаем курсор левее базового пути "0:\"
                        if (saveCursorX > 3) saveCursorX--;
                    }
                    else if (saveKey.Key == ConsoleKeyEx.RightArrow)
                    {
                        if (saveCursorX < inputSavePath.Length) saveCursorX++;
                    }
                    else if (saveKey.KeyChar >= 32)
                    {
                        // Ограничение на длину пути, чтобы не вылезло за рамки поля ввода
                        if (inputSavePath.Length < 28)
                        {
                            inputSavePath = inputSavePath.Insert(saveCursorX, saveKey.KeyChar.ToString());
                            saveCursorX++;
                        }
                    }
                }
                // Если открыто окно сохранения, мышь и обычная клавиатура блокнота не работают!
                return;
            }


            // --- ОБРАБОТКА МЫШИ (ОБЫЧНЫЙ РЕЖИМ) ---
            if (MouseManager.MouseState == MouseState.Left && !isMousePressed)
            {
                isMousePressed = true;

                // Клик по [X]
                if (mX >= windowX + width - 40 && mX <= windowX + width && mY >= currentY && mY <= currentY + 30)
                {
                    isClosing = true;
                    targetY = 600;
                }
                // Клик по [MAX/MIN]
                else if (mX >= windowX + width - 80 && mX <= windowX + width - 40 && mY >= currentY && mY <= currentY + 30)
                {
                    if (isFullscreen) SetNormalMode();
                    else SetFullscreenMode();
                }
                // Клик по [SAVE] - ТЕПЕРЬ ОТКРЫВАЕТ ДИАЛОГ
                else if (mX >= windowX + width - 170 && mX <= windowX + width - 90 && mY >= currentY && mY <= currentY + 30)
                {
                    inputSavePath = currentFilePath; // Подставляем текущий путь по умолчанию
                    saveCursorX = inputSavePath.Length; // Ставим курсор в самый конец
                    isSaveDialogOpen = true;
                }
                // Клик по текстовой области
                else if (mX > windowX + 10 && mX < windowX + width - 10 && mY > currentY + 40 && mY < currentY + height - 20)
                {
                    cursorY = Math.Max(0, Math.Min(lines.Count - 1, (mY - (currentY + 40)) / 20));
                    cursorX = Math.Max(0, Math.Min(lines[cursorY].Length, (mX - (windowX + 10)) / 8));
                    showCursor = true;
                    cursorTimer = 0;
                }
            }
            else if (MouseManager.MouseState == MouseState.None)
            {
                isMousePressed = false;
            }

            // --- ОБРАБОТКА КЛАВИАТУРЫ (ОБЫЧНЫЙ РЕЖИМ) ---
            if (KeyboardManager.TryReadKey(out KeyEvent key))
            {
                showCursor = true;
                cursorTimer = 0;

                // ВЫЧИСЛЯЕМ МАКСИМУМЫ (защита от бага с вылезанием за рамки)
                int maxCharsPerLine = (width - 30) / 8; // Сколько символов влезет по ширине
                int maxLines = (height - 60) / 20;      // Сколько строк влезет по высоте

                if (key.Key == ConsoleKeyEx.Enter)
                {
                    // Не даем создать новую строку, если достигли дна окна
                    if (lines.Count < maxLines)
                    {
                        string currentLine = lines[cursorY];
                        lines[cursorY] = currentLine.Substring(0, cursorX);
                        lines.Insert(cursorY + 1, currentLine.Substring(cursorX));
                        cursorY++;
                        cursorX = 0;
                    }
                }
                else if (key.Key == ConsoleKeyEx.Backspace)
                {
                    if (cursorX > 0)
                    {
                        lines[cursorY] = lines[cursorY].Remove(--cursorX, 1);
                    }
                    else if (cursorY > 0)
                    {
                        // Склеиваем строки, только если новая строка не станет слишком длинной
                        if (lines[cursorY - 1].Length + lines[cursorY].Length <= maxCharsPerLine)
                        {
                            cursorX = lines[cursorY - 1].Length;
                            lines[cursorY - 1] += lines[cursorY];
                            lines.RemoveAt(cursorY);
                            cursorY--;
                        }
                    }
                }
                else if (key.Key == ConsoleKeyEx.LeftArrow)
                {
                    if (cursorX > 0) cursorX--;
                    else if (cursorY > 0) { cursorY--; cursorX = lines[cursorY].Length; }
                }
                else if (key.Key == ConsoleKeyEx.RightArrow)
                {
                    if (cursorX < lines[cursorY].Length) cursorX++;
                    else if (cursorY < lines.Count - 1) { cursorY++; cursorX = 0; }
                }
                else if (key.Key == ConsoleKeyEx.UpArrow)
                {
                    if (cursorY > 0) { cursorY--; cursorX = Math.Min(cursorX, lines[cursorY].Length); }
                }
                else if (key.Key == ConsoleKeyEx.DownArrow)
                {
                    if (cursorY < lines.Count - 1) { cursorY++; cursorX = Math.Min(cursorX, lines[cursorY].Length); }
                }
                else if (key.KeyChar >= 32)
                {
                    // Разрешаем печатать символ, ТОЛЬКО если строка еще не уперлась в правый край
                    if (lines[cursorY].Length < maxCharsPerLine)
                    {
                        lines[cursorY] = lines[cursorY].Insert(cursorX++, key.KeyChar.ToString());
                    }
                }
            }
        }
    }
}