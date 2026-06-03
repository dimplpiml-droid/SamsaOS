using System;
using System.IO;
using System.Drawing;
using Cosmos.System;
using Cosmos.System.Graphics;

namespace SamsaOS.GUI
{
    public static class NotepadGUI
    {
        public static bool isActive = false;
        public static bool isFullscreen = false;
        private static bool isClosing = false; // Флаг закрытия окна

        // Стандартные координаты
        private static int normalX = 50;
        private static int normalY = 50;
        private static int normalWidth = 500;
        private static int normalHeight = 350;

        // Текущие координаты для отрисовки
        private static int currentY = 600;
        private static int windowX = 50;
        private static int width = 500;
        private static int height = 350;

        private static int cursorX = 0; // Позиция символа в строке
        private static int cursorY = 0; // Номер строки
        private static bool showCursor = true;
        private static int cursorTimer = 0;

        // ЦЕЛЕВЫЕ координаты (куда окно стремится каждый кадр)
        private static int targetY = 50;
        private static int targetX = 50;
        private static int targetWidth = 500;
        private static int targetHeight = 350;

        public static string currentText = "";
        public static string currentFilePath = @"0:\note.txt";

        private static bool isMousePressed = false;

        public static void Open(string path = @"0:\note.txt")
        {
            currentFilePath = path;
            isFullscreen = false;
            isClosing = false;

            // Стартуем за экраном
            currentY = 600;
            windowX = normalX;
            width = normalWidth;
            height = normalHeight;

            SetNormalMode(); // Устанавливаем цели на нормальный размер
            isActive = true;

            if (File.Exists(currentFilePath)) currentText = File.ReadAllText(currentFilePath);
            else currentText = "";
        }

        private static void SetFullscreenMode()
        {
            targetX = 0;
            targetY = 0;
            targetWidth = 800;
            targetHeight = 530; // Оставляем место для панели задач
            isFullscreen = true;
        }

        private static void SetNormalMode()
        {
            targetX = normalX;
            targetY = normalY;
            targetWidth = normalWidth;
            targetHeight = normalHeight;
            isFullscreen = false;
        }

        // Вспомогательный метод для плавной анимации 
        private static int SmoothStep(int current, int target)
        {
            if (current == target) return current;
            int diff = target - current;
            int step = diff / 4; // Скорость анимации (чем больше число, тем медленнее)
            if (step == 0) step = diff > 0 ? 1 : -1; // Чтобы не зависло на 1 пикселе
            return current + step;
        }

        public static void Render(Canvas canvas)
        {
            if (!isActive) return;

            // 1. ЛОГИКА АНИМАЦИИ (Плавное изменение всех параметров)
            windowX = SmoothStep(windowX, targetX);
            currentY = SmoothStep(currentY, targetY);
            width = SmoothStep(width, targetWidth);
            height = SmoothStep(height, targetHeight);

            // Если окно закрывалось и уже уехало за экран — отключаем его
            if (isClosing && currentY >= 590)
            {
                isActive = false;
                isClosing = false;
                return;
            }

            // 2. ОТРИСОВКА ОКНА
            // Тень (не рисуем в полноэкранном режиме)
            if (!isFullscreen)
            {
                canvas.DrawFilledRectangle(new Pen(Color.FromArgb(20, 20, 20)), windowX + 5, currentY + 5, width, height);
            }

            // Фон блокнота
            canvas.DrawFilledRectangle(new Pen(Color.White), windowX, currentY, width, height);

            // Шапка окна
            canvas.DrawFilledRectangle(new Pen(Color.FromArgb(45, 45, 48)), windowX, currentY, width, 30);
            canvas.DrawString($"Samsa Notepad - {currentFilePath}", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), windowX + 10, currentY + 8);

            // Кнопка [SAVE]
            canvas.DrawFilledRectangle(new Pen(Color.SeaGreen), windowX + width - 170, currentY, 80, 30);
            canvas.DrawString("SAVE", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), windowX + width - 150, currentY + 8);

            // Кнопка [MAX/MIN]
            canvas.DrawFilledRectangle(new Pen(Color.Goldenrod), windowX + width - 80, currentY, 40, 30);
            string maxMinIcon = isFullscreen ? "[-]" : "[+]";
            canvas.DrawString(maxMinIcon, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), windowX + width - 70, currentY + 8);

            // Кнопка [X]
            canvas.DrawFilledRectangle(new Pen(Color.DarkRed), windowX + width - 40, currentY, 40, 30);
            canvas.DrawString("X", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), windowX + width - 25, currentY + 8);

            // 3. ОТРИСОВКА ТЕКСТА
            string[] lines = currentText.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                // Рисуем текст только если он помещается по высоте И по ширине
                if (currentY + 40 + (i * 20) < currentY + height - 20)
                {
                    canvas.DrawString(lines[i], Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.Black), windowX + 10, currentY + 40 + (i * 20));
                }
            }

            // 4. ОБРАБОТКА ВВОДА
            HandleInput();
        }

        private static void HandleInput()
        {
            if (MouseManager.MouseState == MouseState.Left)
            {
                if (!isMousePressed)
                {
                    isMousePressed = true;
                    int mX = (int)MouseManager.X;
                    int mY = (int)MouseManager.Y;

                    // Клик по [X] - ТЕПЕРЬ ЗАПУСКАЕТ АНИМАЦИЮ ВНИЗ
                    if (mX >= windowX + width - 40 && mX <= windowX + width && mY >= currentY && mY <= currentY + 30)
                    {
                        isClosing = true;
                        targetY = 600; // Отправляем окно за пределы экрана
                    }

                    // Клик по [MAX/MIN]
                    if (mX >= windowX + width - 80 && mX <= windowX + width - 40 && mY >= currentY && mY <= currentY + 30)
                    {
                        if (isFullscreen) SetNormalMode();
                        else SetFullscreenMode();
                    }

                    // Клик по [SAVE]
                    if (mX >= windowX + width - 170 && mX <= windowX + width - 90 && mY >= currentY && mY <= currentY + 30)
                    {
                        if (File.Exists(currentFilePath)) File.Delete(currentFilePath);
                        File.WriteAllText(currentFilePath, currentText);
                    }
                }
            }
            else
            {
                isMousePressed = false;
            }

            // ... Блок клавиатуры остается без изменений ...
            if (KeyboardManager.TryReadKey(out KeyEvent key))
            {
                if (key.Key == ConsoleKeyEx.Backspace)
                {
                    if (currentText.Length > 0) currentText = currentText.Remove(currentText.Length - 1);
                }
                else if (key.Key == ConsoleKeyEx.Enter)
                {
                    currentText += '\n';
                }
                else
                {
                    if (char.IsLetterOrDigit(key.KeyChar) || char.IsPunctuation(key.KeyChar) || char.IsSymbol(key.KeyChar) || key.KeyChar == ' ')
                    {
                        currentText += key.KeyChar;
                    }
                }
            }
        }
    }
}