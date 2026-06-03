using Cosmos.System.FileSystem;
using Cosmos.System.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using SamsaOS.Apps;
using Sys = Cosmos.System;

namespace SamsaOS
{
    public class Kernel : Sys.Kernel
    {
        private bool desktopStarted = false;

        private int frameCounter = 0;
        private const int FrameDelayMs = 33; // ~30 FPS target - smoother cursor/animation while keeping draw load reasonable
        private const int GcEveryFrames = 300;
        private const int GcEveryFramesEditor = 500;

        // Clock caching to avoid formatting + DrawString + RTC every single frame
        private static int lastClockSecond = -1;
        private static string lastClockString = "";

        // Hybrid redraw: do expensive full base (wallpaper/clear + logo + desktop icons) only periodically
        // to reduce CPU/GFX load while still clearing cursor trails reasonably fast.
        private static bool forceFullDesktopRedraw = true;
        private static int desktopFrame = 0;
        private const int FullRedrawEvery = 3; // full expensive redraw every N frames (reduces big blits + wallpaper work)

        // инициализация файловой системы
        Sys.FileSystem.CosmosVFS vfs;
        CommandManager cmdManager;

        public static Canvas canvas;
        private static bool isGuiActive = false;
        private static bool isMivEditorActive = false;

        private const int EditorWindowX = 70;
        private const int EditorWindowY = 55;
        private const int EditorWindowWidth = 660;
        private const int EditorWindowHeight = 500;


        // Логика файлов и страниц
        private static List<string> allFiles = new List<string>();
        private static int currentPage = 0;
        private const int FilesPerPage = 10;
        private static int selectedFileIndex = -1; // Индекс выбранного файла на текущей странице

        // Ограничение для защиты от дребезга мыши (клик срабатывает один раз)
        private static bool isMousePressed = false;

        public struct FSObject
        {
            public string Name;
            public bool IsDirectory;
        }

        private static string guiCurrentDirectory = @"0:\";
        private static List<FSObject> allFSObjects = new List<FSObject>();


        // DESKTOP
        private static Bitmap wallpaper;
        private static bool isDesktopActive = false;

        // Pre-allocated pens to reduce allocations in hot render path (helps lag/GC)
        private static readonly Pen CyanPen = new Pen(Color.Cyan);
        private static readonly Pen WhitePen = new Pen(Color.White);
        private static readonly Pen TaskbarBgPen = new Pen(Color.FromArgb(10, 15, 30));
        private static readonly Pen DarkRedPen = new Pen(Color.DarkRed);
        private static readonly Pen DarkBluePen = new Pen(Color.DarkBlue);
        private static readonly Pen YellowPen = new Pen(Color.Yellow);
        private static readonly Pen GreenPen = new Pen(Color.Green);
        private static readonly Pen GrayPen = new Pen(Color.Gray);
        private static readonly Pen RedPen = new Pen(Color.Red);
        private static readonly Pen BluePen = new Pen(Color.Blue);
        private static readonly Pen OpenBtnPen = new Pen(Color.FromArgb(52, 152, 219));
        private static readonly Pen DeleteBtnPen = new Pen(Color.FromArgb(231, 76, 60));
        private static readonly Pen BlackPen = new Pen(Color.Black);
        private static readonly Pen LightGrayPen = new Pen(Color.LightGray);
        private static readonly Pen CursorPen = new Pen(Color.White);

        public static void StartDesktop()
        {
            try
            {
                if (File.Exists(@"0:\wallpaper.bmp"))
                {
                    wallpaper = new Bitmap(@"0:\wallpaper.bmp");
                }
            }
            catch
            {
                wallpaper = null;
            }
            canvas = FullScreenCanvas.GetFullScreenCanvas(new Mode(800, 600, ColorDepth.ColorDepth32));


            Sys.MouseManager.ScreenWidth = 800;
            Sys.MouseManager.ScreenHeight = 600;

            Sys.MouseManager.MouseState = Sys.MouseState.None;

            isGuiActive = false;
            isMivEditorActive = false;
            isDesktopActive = true;
            forceFullDesktopRedraw = true;
        }

        private void RenderDesktop()
        {
            try
            {
                desktopFrame++;
                bool doFull = forceFullDesktopRedraw || (desktopFrame % FullRedrawEvery == 0);
                if (doFull)
                {
                    if (wallpaper != null)
                    {
                        canvas.DrawImage(wallpaper, 0, 0);
                    }
                    else
                    {
                        canvas.Clear(Color.FromArgb(25, 35, 60));
                    }

                    // Логотип (static, only on full passes)
                    canvas.DrawString("SAMSA OS", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, CyanPen,340, 250);
                    canvas.DrawString("Version 0.7", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 355, 280);

                    forceFullDesktopRedraw = false;
                }

                // Top bars - cheap, draw every frame (or they would be wiped on full base frames)
                canvas.DrawFilledRectangle(CyanPen, 0, 0, 800, 3);
                canvas.DrawFilledRectangle(CyanPen, 0, 597, 800, 3);


                int mX = (int)Sys.MouseManager.X;
                int mY = (int)Sys.MouseManager.Y;

                if (doFull)
                {
                    // Desktop icons - static, only on full passes (remain visible otherwise)
                    // ===== Иконка FILES =====
                    canvas.DrawFilledRectangle(YellowPen, 60, 80, 50, 50);
                    canvas.DrawString("FILES", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 60, 140);

                    // ===== Иконка CONSOLE =====
                    canvas.DrawFilledRectangle(GreenPen, 60, 220, 50, 50);
                    canvas.DrawString("CONSOLE", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 55, 280);
                }


                // Панель задач
                canvas.DrawFilledRectangle(TaskbarBgPen, 0, 530, 800, 70);

                // Верхняя линия панели
                canvas.DrawFilledRectangle(CyanPen, 0, 530, 800, 2);

                // ===== Часы (обновляем только при смене секунды) =====
                int currentSec = Cosmos.HAL.RTC.Second;
                if (currentSec != lastClockSecond)
                {
                    lastClockString = Cosmos.HAL.RTC.Hour.ToString("00") + ":" + Cosmos.HAL.RTC.Minute.ToString("00") + ":" + currentSec.ToString("00");
                    lastClockSecond = currentSec;
                }
                // Небольшой фон под часами (чтобы старые цифры не оставляли артефакты)
                canvas.DrawFilledRectangle(TaskbarBgPen, 695, 548, 100, 22);
                canvas.DrawString(lastClockString, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 700, 555);

                // Кнопка питания
                canvas.DrawFilledRectangle(DarkRedPen, 10, 540, 50, 50);                
                canvas.DrawFilledRectangle(WhitePen, 33, 548, 4, 14);
                canvas.DrawFilledRectangle(WhitePen, 20, 560, 30, 4);
                canvas.DrawFilledRectangle(WhitePen, 20, 560, 4, 20);
                canvas.DrawFilledRectangle(WhitePen, 46, 560, 4, 20);
                canvas.DrawFilledRectangle(WhitePen, 20, 576, 30, 4);

                // Кнопка перезагрузки
                canvas.DrawFilledRectangle(DarkBluePen, 70, 540, 50, 50);
                canvas.DrawString("RE",Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 88, 558);

                // ===== Клики =====

                if (Sys.MouseManager.MouseState == Sys.MouseState.Left)
                {
                    if (!isMousePressed)
                    {
                        isMousePressed = true;

                        // FILES

                        if (mX >= 60 && mX <= 110 && mY >= 80 && mY <= 130)
                        {
                            isDesktopActive = false;
                            UpdateFileList();
                            currentPage = 0;
                            selectedFileIndex = -1;
                            isGuiActive = true;
                            isMivEditorActive = false;
                            return;
                        }

                        // CONSOLE

                        if (mX >= 60 && mX <= 110 && mY >= 220 && mY <= 270)
                        {
                            isDesktopActive = false;
                            canvas.Disable();
                            Console.Clear();
                            return;
                        }

                        // SHUTDOWN
                        if (mX >= 10 && mX <= 60 && mY >= 540 && mY <= 590)
                        {
                            canvas.Disable();

                            Console.Clear();
                            Console.WriteLine("Shutting down SamsaOS...");

                            Cosmos.System.Power.Shutdown();
                            return;
                        }

                        // REBOOT
                        if (mX >= 70 && mX <= 120 && mY >= 540 && mY <= 590)
                        {
                            canvas.Disable();

                            Console.Clear();
                            Console.WriteLine("Rebooting SamsaOS...");

                            Cosmos.System.Power.Reboot();
                            return;
                        }
                    }
                }
                else
                {
                    isMousePressed = false;
                }

                // ===== Курсор =====

                canvas.DrawFilledRectangle(CursorPen, mX, mY, 4, 4);

                EndFrame(false);
            }
            catch (Exception ex)
            {
                isDesktopActive = false;

                canvas?.Disable();

                Console.WriteLine($"[Desktop Crash]: {ex.Message}");
            }
        }








        private void DrawWrappedText(string text, int startX, int startY, int maxCharsPerLine)
        {
            int currentY = startY;
            string remainingText = text;

            // Если текст пустой, просто выходим
            if (string.IsNullOrEmpty(text)) return;

            // Режем текст на куски заданной длины
            while (remainingText.Length > maxCharsPerLine)
            {
                string line = remainingText.Substring(0, maxCharsPerLine);
                canvas.DrawString(line, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.Yellow), startX, currentY);

                remainingText = remainingText.Substring(maxCharsPerLine);
                currentY += 20; // Смещаемся на 20 пикселей вниз для следующей строки
            }

            // Дорисовываем оставшийся хвостик текста
            if (remainingText.Length > 0)
            {
                canvas.DrawString(remainingText, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.Yellow), startX, currentY);
            }
        }

        public static void StartGuiMode(string currentConsolePath)
        {
            Console.Beep(880, 150); // Высокий чистый тон (А5)

            Console.Beep(1046, 250); // Финальный акцент (C6)

            canvas = FullScreenCanvas.GetFullScreenCanvas(new Mode(800, 600, ColorDepth.ColorDepth32));
            Sys.MouseManager.ScreenWidth = 800;
            Sys.MouseManager.ScreenHeight = 600;
            guiCurrentDirectory = currentConsolePath;

            Sys.MouseManager.MouseState = Sys.MouseState.None;

            UpdateFileList(); // Загружаем список файлов с диска
            currentPage = 0;
            selectedFileIndex = -1;
            isGuiActive = true;
            isMivEditorActive = false;
        }

        // Обновление списка файлов из корня диска
        private static void UpdateFileList()
        {
            allFSObjects.Clear();
            try
            {
                // 1. Считываем папки в текущей директории GUI
                if (Directory.Exists(guiCurrentDirectory))
                {
                    string[] dirs = Directory.GetDirectories(guiCurrentDirectory);
                    foreach (var dir in dirs)
                    {
                        // Получаем только чистое имя папки, например "documents" вместо "0:\documents\"
                        string cleanName = Path.GetFileName(dir.TrimEnd('\\'));

                        if (!string.IsNullOrEmpty(cleanName))
                        {
                            allFSObjects.Add(new FSObject { Name = cleanName, IsDirectory = true });
                        }
                    }

                    // 2. Считываем файлы в текущей директории GUI
                    string[] files = Directory.GetFiles(guiCurrentDirectory);
                    foreach (var file in files)
                    {
                        // Получаем чистое имя файла, например "test.txt"
                        string cleanName = Path.GetFileName(file);

                        if (!string.IsNullOrEmpty(cleanName))
                        {
                            allFSObjects.Add(new FSObject { Name = cleanName, IsDirectory = false });
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                // Если произойдет ошибка чтения, мы увидим её до переключения в графику
                Console.WriteLine($"[VFS Read Error]: {ex.Message}");
            }
        }

        public static void WaitSeconds(int seconds)
        {
            Thread.Sleep(seconds * 1000);
        }

        private void EndFrame(bool editorOpen)
        {
            canvas.Display();
            Thread.Sleep(FrameDelayMs);

            frameCounter++;
            int gcInterval = editorOpen ? GcEveryFramesEditor : GcEveryFrames;
            if (frameCounter >= gcInterval)
            {
                Cosmos.Core.Memory.Heap.Collect();
                frameCounter = 0;
            }
        }

        private void RenderGui()
        {
            try
            {
                // 1. Сплошной серый фон всего экрана
                canvas.Clear(Color.DarkGray);

                // Кнопка возврата на уровень вверх [^] возле пути
                canvas.DrawFilledRectangle(GrayPen, 20, 15, 35, 25);
                canvas.DrawString("[^]", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 28, 20);

                // Показ текущей директории сверху экрана
                canvas.DrawString($"Path: {guiCurrentDirectory}", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 70, 20);

                // Маленькая кнопка выхода [X] в самом углу
                canvas.DrawFilledRectangle(RedPen, 750, 15, 20, 20);
                canvas.DrawString("X", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 756, 18);

                // 2. ВЫВОД СПИСКА ОБЪЕКТОВ (Папки и файлы)
                int startIdx = currentPage * FilesPerPage;
                int endIdx = Math.Min(startIdx + FilesPerPage, allFSObjects.Count);
                int yOffset = 80;

                for (int i = startIdx; i < endIdx; i++)
                {
                    int localIndex = i - startIdx;

                    // Синее подчеркивание для выбранного элемента
                    if (localIndex == selectedFileIndex)
                    {
                        canvas.DrawFilledRectangle(BluePen, 20, yOffset + 15, 350, 2);
                    }

                    // Вывод типа объекта и его имени
                    string prefix = allFSObjects[i].IsDirectory ? "[DIR]  " : "[FILE] ";
                    canvas.DrawString(prefix + allFSObjects[i].Name, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 25, yOffset);
                    yOffset += 40;
                }

                // 3. СТРЕЛОЧКИ МНОГОСТРАНИЧНОСТИ СНИЗУ
                // Левая стрелка
                canvas.DrawFilledRectangle(GrayPen, 25, 480, 40, 30);
                canvas.DrawString("<-", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 37, 488);

                // Номер страницы
                int maxPage = Math.Max(1, (int)Math.Ceiling((double)allFSObjects.Count / FilesPerPage));
                canvas.DrawString($"{currentPage + 1} / {maxPage}", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 85, 488);

                // Right arrow
                canvas.DrawFilledRectangle(GrayPen, 150, 480, 40, 30);
                canvas.DrawString("->", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 162, 488);


                // 4. КНОПКИ УПРАВЛЕНИЯ (Справа)
                // Кнопка "Открыть / Войти" (Синяя)
                canvas.DrawFilledRectangle(OpenBtnPen, 500, 100, 140, 40);
                canvas.DrawString("Open / Enter", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 520, 112);

                // Кнопка "Удалить" (Красная)
                canvas.DrawFilledRectangle(DeleteBtnPen, 500, 160, 140, 40);
                canvas.DrawString("Delete", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, WhitePen, 545, 172);

                if (isMivEditorActive)
                {
                    ProcessEditorKeyboard();
                    if (isMivEditorActive)
                    {
                        MivEditor.RenderGui(canvas, EditorWindowX, EditorWindowY, EditorWindowWidth, EditorWindowHeight);
                    }
                }

                // 5. ОБРАБОТКА КЛИКОВ МЫШИ
                int mX = (int)Sys.MouseManager.X;
                int mY = (int)Sys.MouseManager.Y;

                if (Sys.MouseManager.MouseState == Sys.MouseState.Left)
                {
                    if (!isMousePressed)
                    {
                        isMousePressed = true;

                        if (isMivEditorActive)
                        {
                            HandleEditorMouse(mX, mY);
                        }
                        else
                        {

                            // Клик по [X] (Выход в консоль)
                            if (mX >= 750 && mX <= 770 && mY >= 15 && mY <= 35)
                            {
                                isGuiActive = false;
                                isMivEditorActive = false;
                                canvas.Disable();
                                Console.Clear();
                                StartDesktop();
                                return;
                            }

                            // Клик по стрелочке возврата [^] (Переход в родительскую папку)
                            if (mX >= 20 && mX <= 55 && mY >= 15 && mY <= 40)
                            {
                                if (guiCurrentDirectory != @"0:\")
                                {
                                    string sub = guiCurrentDirectory.Substring(0, guiCurrentDirectory.Length - 1);
                                    guiCurrentDirectory = sub.Substring(0, sub.LastIndexOf(@"\") + 1);
                                    selectedFileIndex = -1;
                                    currentPage = 0;
                                    UpdateFileList();
                                }
                            }

                            // Выбор элемента кликом по списку
                            if (mX >= 20 && mX <= 400 && mY >= 70 && mY <= 460)
                            {
                                int clickedLine = (mY - 70) / 40;
                                if (clickedLine >= 0 && clickedLine < (endIdx - startIdx))
                                {
                                    selectedFileIndex = clickedLine;
                                }
                            }

                            // Листание страниц стрелками
                            if (mX >= 25 && mX <= 65 && mY >= 480 && mY <= 510)
                            {
                                if (currentPage > 0) { currentPage--; selectedFileIndex = -1; }
                            }
                            if (mX >= 150 && mX <= 190 && mY >= 480 && mY <= 510)
                            {
                                if ((currentPage + 1) * FilesPerPage < allFSObjects.Count) { currentPage++; selectedFileIndex = -1; }
                            }

                            // Действие: ОТКРЫТЬ / ВОЙТИ
                            if (mX >= 500 && mX <= 640 && mY >= 100 && mY <= 140)
                            {
                                if (selectedFileIndex != -1)
                                {
                                    int globalIdx = (currentPage * FilesPerPage) + selectedFileIndex;
                                    FSObject selectedObj = allFSObjects[globalIdx];

                                    if (selectedObj.IsDirectory)
                                    {
                                        // Вход в папку
                                        guiCurrentDirectory = guiCurrentDirectory + selectedObj.Name + @"\";
                                        selectedFileIndex = -1;
                                        currentPage = 0;
                                        UpdateFileList();
                                    }
                                    else
                                    {
                                        // Открываем файл в графическом редакторе
                                        string fullPath = guiCurrentDirectory + selectedObj.Name;
                                        OpenMivEditor(fullPath);
                                    }
                                }
                            }

                            // Действие: УДАЛИТЬ (с проверкой на пустоту директории)
                            if (mX >= 500 && mX <= 640 && mY >= 160 && mY <= 200)
                            {
                                if (selectedFileIndex != -1)
                                {
                                    int globalIdx = (currentPage * FilesPerPage) + selectedFileIndex;
                                    FSObject selectedObj = allFSObjects[globalIdx];
                                    string fullPath = guiCurrentDirectory + selectedObj.Name;

                                    try
                                    {
                                        if (selectedObj.IsDirectory)
                                        {
                                            // Флаг false запрещает удаление, если папка НЕ пустая
                                            Directory.Delete(fullPath, false);
                                        }
                                        else
                                        {
                                            File.Delete(fullPath);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string msg = ex.Message.ToLower().Contains("empty")
                                            ? "Error: Directory is not empty!"
                                            : $"Error: {ex.Message}";
                                        canvas.DrawString(msg, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, RedPen, 500, 230);
                                        canvas.Display();
                                        WaitSeconds(2);
                                    }

                                    selectedFileIndex = -1;
                                    UpdateFileList();
                                    if (currentPage * FilesPerPage >= allFSObjects.Count && currentPage > 0) currentPage--;
                                }
                            }
                        }
                    }
                }
                else
                {
                    isMousePressed = false;
                }

                // 6. Отрисовка курсора мыши (простая белая точка)
                canvas.DrawFilledRectangle(CursorPen, mX, mY, 4, 4);

                EndFrame(isMivEditorActive);
            }
            catch (Exception ex)
            {
                isGuiActive = false;
                canvas?.Disable();
                Console.WriteLine($"[GUI Crash]: {ex.Message}");
            }
        }

        private static void ProcessEditorKeyboard()
        {
            try
            {
                while (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.Escape)
                    {
                        CloseMivEditor();
                        return;
                    }

                    if (key.Key == ConsoleKey.F2)
                    {
                        MivEditor.SaveAndClose();
                        CloseMivEditor();
                        UpdateFileList();
                        return;
                    }

                    if (key.Key == ConsoleKey.S && key.Modifiers == ConsoleModifiers.Control)
                    {
                        MivEditor.SaveSilently();
                        continue;
                    }

                    MivEditor.HandleGuiKey(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Editor Input Error]: {ex.Message}");
            }
        }

        private static void HandleEditorMouse(int mX, int mY)
        {
            int saveX = EditorWindowX + EditorWindowWidth - 170;
            int closeX = EditorWindowX + EditorWindowWidth - 88;
            int buttonY = EditorWindowY + 4;

            if (mX >= closeX && mX <= closeX + 70 && mY >= buttonY && mY <= buttonY + 24)
            {
                CloseMivEditor();
                return;
            }

            if (mX >= saveX && mX <= saveX + 70 && mY >= buttonY && mY <= buttonY + 24)
            {
                MivEditor.SaveSilently();
                UpdateFileList();
            }
        }

        private static void OpenMivEditor(string fullPath)
        {
            MivEditor.OpenFile(fullPath);
            isMivEditorActive = true;
            isMousePressed = false;
            Sys.MouseManager.MouseState = Sys.MouseState.None;
        }

        private static void CloseMivEditor()
        {
            isMivEditorActive = false;
            isMousePressed = false;
            Sys.MouseManager.MouseState = Sys.MouseState.None;
        }

        protected override void BeforeRun()
        {
            // 1. Регистрируем VFS
            vfs = new CosmosVFS();
            Sys.FileSystem.VFS.VFSManager.RegisterVFS(vfs);
            //try
            //{
            //    wallpaper = new Bitmap(@"0:\wallpaper.bmp");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Wallpaper load error: " + ex.Message);
            //}

            // 2. ЖЕСТКИЙ СКРИПТ ФОРМАТИРОВАНИЯ
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[VFS] Checking storage device...");

                // Проверяем, видит ли ядро хоть один физический диск
                var disks = Sys.FileSystem.VFS.VFSManager.GetDisks();
                if (disks.Count > 0)
                {
                    var disk = disks[0]; // Берем наш новый SATA/IDE диск

                    // Если на нем чисто — принудительно размечаем
                    if (disk.Partitions.Count == 0)
                    {
                        Console.WriteLine("[VFS] Empty disk found. Cleaving partitions...");
                        disk.Clear();

                        // Создаем раздел на 500 МБ (или сколько позволяет диск)
                        disk.CreatePartition(500);

                        Console.WriteLine("[VFS] Partition created. Formatting to FAT32...");
                        disk.FormatPartition(0, "FAT32", true);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[VFS] Success! PLEASE CLOSE VM AND RUN F5 AGAIN!");
                        Console.ResetColor();

                        Stop(); // Замораживаем систему, требуя перезагрузки
                        return;
                    }
                    else
                    {
                        Console.WriteLine("[VFS] Hard drive partition detected layout.");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[VFS ERROR] NO PHYSICAL SATA/IDE DISKS DETECTED IN VM!");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VFS CRITICAL ERROR]: {ex.Message}");
            }

            // 3. Обработчик команд (ваш код далее...)
            cmdManager = new CommandManager();
            Console.Clear();

            // Воспроизведение звука загрузки
            Console.Beep(440, 200);
            Console.Beep(554, 200);
            Console.Beep(659, 300);

            // Вывод логотипа
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"  ____                               ___  ____  ");
            Console.WriteLine(@" / ___|  __ _ _ __ ___  ___  __ _   / _ \/ ___| ");
            Console.WriteLine(@" \___ \ / _` | '_ ` _ \/ __|/ _` | | | | \___ \ ");
            Console.WriteLine(@"  ___) | (_| | | | | | \__ \ (_| | | |_| |___) |");
            Console.WriteLine(@" |____/ \__,_|_| |_| |_|___/\__,_|  \___/|____/ ");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" Welcome to SamsaOS v0.7!");
            Console.WriteLine(" Type 'help' for the list of commands.");
            Console.WriteLine("========================================");

        }

        protected override void Run()
        {
            if (!desktopStarted)
            {
                desktopStarted = true;
                StartDesktop();
            }

            if (isDesktopActive)
            {
                RenderDesktop();
                return;
            }

            if (isGuiActive)
            {
                RenderGui();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"root@samsa:{cmdManager.CurrentDirectory}> ");
            Console.ResetColor();

            string input = "";
            ConsoleKeyInfo key;

            while (true)
            {
                key = Console.ReadKey(true); //читать нажатие без вывода на экран

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.Tab)
                {
                    //разбиваем строку, чтобы автодополнять только последнее введенное слово
                    string[] parts = input.Split(' ');
                    string lastPart = parts[parts.Length - 1];

                    if (!string.IsNullOrEmpty(lastPart))
                    {
                        // Ищем совпадение
                        string completion = cmdManager.AutoComplete(lastPart);
                        if (!string.IsNullOrEmpty(completion))
                        {
                            // дописатт недостающий кусок в переменную и на экран
                            input += completion;
                            Console.Write(completion);
                        }
                    }
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                    {
                        input = input.Remove(input.Length - 1);

                        if (Console.CursorLeft > 0)
                        {
                            Console.CursorLeft--;
                            Console.Write(' ');
                            Console.CursorLeft--;
                        }
                    }
                }
                else
                {
                    // защита от мусорных символов                 
                    if (key.KeyChar >= 32 && key.KeyChar <= 126)
                    {
                        input += key.KeyChar;
                        Console.Write(key.KeyChar);
                    }
                }
            }

            // Если строка не пустая - отправляем на обработку
            if (!string.IsNullOrWhiteSpace(input)) cmdManager.ProcessInput(input);
        }
    }
}
