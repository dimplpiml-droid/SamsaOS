using Cosmos.System.FileSystem;
using Cosmos.System.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml.Linq;
using Sys = Cosmos.System;

namespace SamsaOS
{
    public class Kernel : Sys.Kernel
    {
        private bool desktopStarted = false;

        private int frameCounter = 0;

        // инициализация файловой системы
        Sys.FileSystem.CosmosVFS vfs;
        CommandManager cmdManager;

        public static Canvas canvas;
        private static bool isGuiActive = false;


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
            isDesktopActive = true;
        }

        private void RenderDesktop()
        {
            try
            {
                canvas.Clear(Color.FromArgb(25, 35, 60));

                if (wallpaper != null)
                {
                    canvas.DrawImage(wallpaper, 0, 0);
                }

                // Логотип
                canvas.DrawString("SAMSA OS", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.Cyan), 340, 250);
                canvas.DrawString("Version 0.7", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 355, 280);

                // ===== ЛОГОТИП SAMSA OS =====
                //canvas.DrawString("███████╗ █████╗ ███╗   ███╗███████╗",Cosmos.System.Graphics.Fonts.PCScreenFont.Default,new Pen(Color.Cyan),130,180);
                //canvas.DrawString("██╔════╝██╔══██╗████╗ ████║██╔════╝",Cosmos.System.Graphics.Fonts.PCScreenFont.Default,new Pen(Color.Cyan),130,200);
                //canvas.DrawString("███████╗███████║██╔████╔██║███████╗",Cosmos.System.Graphics.Fonts.PCScreenFont.Default,new Pen(Color.Cyan), 130,220);
                //canvas.DrawString("╚════██║██╔══██║██║╚██╔╝██║╚════██║",Cosmos.System.Graphics.Fonts.PCScreenFont.Default,new Pen(Color.Cyan),130, 240);
                //canvas.DrawString("███████║██║  ██║██║ ╚═╝ ██║███████║",Cosmos.System.Graphics.Fonts.PCScreenFont.Default,new Pen(Color.Cyan), 130,260);
                //canvas.DrawString("╚══════╝╚═╝  ╚═╝╚═╝     ╚═╝╚══════╝",Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.Cyan),130,280);
                //canvas.DrawString("OPERATING SYSTEM",Cosmos.System.Graphics.Fonts.PCScreenFont.Default,new Pen(Color.White),300,320);
                //canvas.DrawString("Version 0.7",Cosmos.System.Graphics.Fonts.PCScreenFont.Default,new Pen(Color.White),340,340);


                canvas.DrawFilledRectangle(new Pen(Color.Cyan), 0, 0, 800, 3);
                canvas.DrawFilledRectangle(new Pen(Color.Cyan), 0, 597, 800, 3);


                int mX = (int)Sys.MouseManager.X;
                int mY = (int)Sys.MouseManager.Y;

                // ===== Иконка FILES =====
                canvas.DrawFilledRectangle(new Pen(Color.Yellow), 60, 80, 50, 50);
                canvas.DrawString("FILES", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 60, 140);

                // ===== Иконка CONSOLE =====
                canvas.DrawFilledRectangle(new Pen(Color.Green), 60, 220, 50, 50);
                canvas.DrawString("CONSOLE", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 55, 280);

                // ===== Иконка NOTEPAD (ВСТАВЛЯЕМ СЮДА) =====
                canvas.DrawFilledRectangle(new Pen(Color.White), 60, 360, 50, 50);
                canvas.DrawString("NOTEPAD", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 55, 420);

                // Панель задач
                canvas.DrawFilledRectangle(new Pen(Color.FromArgb(10, 15, 30)), 0, 530, 800, 70);
                Color.FromArgb(25, 35, 60);

                // Верхняя линия панели
                canvas.DrawFilledRectangle(new Pen(Color.Cyan), 0, 530, 800, 2);

                // ===== Часы =====
                string time = Cosmos.HAL.RTC.Hour.ToString("00") + ":" + Cosmos.HAL.RTC.Minute.ToString("00") + ":" + Cosmos.HAL.RTC.Second.ToString("00");
                canvas.DrawString(time, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 700, 555);

                // Кнопка питания
                canvas.DrawFilledRectangle(new Pen(Color.DarkRed), 10, 540, 50, 50);
                canvas.DrawFilledRectangle(new Pen(Color.White), 33, 548, 4, 14);
                canvas.DrawFilledRectangle(new Pen(Color.White), 20, 560, 30, 4);
                canvas.DrawFilledRectangle(new Pen(Color.White), 20, 560, 4, 20);
                canvas.DrawFilledRectangle(new Pen(Color.White), 46, 560, 4, 20);
                canvas.DrawFilledRectangle(new Pen(Color.White), 20, 576, 30, 4);

                // Кнопка перезагрузки
                canvas.DrawFilledRectangle(new Pen(Color.DarkBlue), 70, 540, 50, 50);
                canvas.DrawString("RE", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 88, 558);


                // ===== Клики =====

                if (Sys.MouseManager.MouseState == Sys.MouseState.Left)
                {
                    if (!isMousePressed)
                    {
                        isMousePressed = true;

                        // Если блокнот открыт, блокируем клики по рабочему столу
                        if (!SamsaOS.GUI.NotepadGUI.isActive)
                        {
                            // FILES
                            if (mX >= 60 && mX <= 110 && mY >= 80 && mY <= 130)
                            {
                                isDesktopActive = false;
                                UpdateFileList();
                                currentPage = 0;
                                selectedFileIndex = -1;
                                isGuiActive = true;
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

                            // NOTEPAD (ВСТАВЛЯЕМ СЮДА)
                            if (mX >= 60 && mX <= 110 && mY >= 360 && mY <= 410)
                            {
                                SamsaOS.GUI.NotepadGUI.Open(@"0:\note.txt"); // Открываем дефолтный файл
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
                        } // Закрывает if (!SamsaOS.GUI.NotepadGUI.isActive)
                    } // Закрывает if (!isMousePressed)
                } // ЗАКРЫВАЕТ if (Sys.MouseManager.MouseState == Sys.MouseState.Left)
                else
                {
                    // Сбрасываем триггер клика, когда кнопка мыши ОТПУЩЕНА
                    isMousePressed = false;
                }

                // ===== ЭТОТ БЛОК ТЕПЕРЬ СНАРУЖИ УСЛОВИЯ КЛИКА =====

                // 1. Обновление анимаций
                SamsaOS.Animation2.Update();

                // 2. Отрисовка Блокнота
                SamsaOS.GUI.NotepadGUI.Render(canvas);

                // 3. Отрисовка мыши (ТЕПЕРЬ ОНА ПОВЕРХ ВСЕГО)
                // Отрисовка мыши "Уголком"
                canvas.DrawFilledRectangle(new Pen(Color.Black), mX, mY, 4, 12);
                canvas.DrawFilledRectangle(new Pen(Color.Black), mX, mY, 12, 4);
                canvas.DrawRectangle(new Pen(Color.White), mX, mY, 4, 12);
                canvas.DrawRectangle(new Pen(Color.White), mX, mY, 12, 4);

                // 4. ВЫВОД БУФЕРА НА ЭКРАН
                canvas.Display();

                // 5. Очистка памяти и задержка
                Cosmos.Core.Memory.Heap.Collect();
                frameCounter++;
                if (frameCounter >= 300)
                {
                    Cosmos.Core.Memory.Heap.Collect();
                    frameCounter = 0;
                }

                for (int i = 0; i < 10000; i++) { sbyte a = 0; }
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
            // Считываем текущую секунду из микросхемы часов материнской платы
            int startSecond = Cosmos.HAL.RTC.Second;
            int targetSecond = (startSecond + seconds) % 60;

            // Крутим пустой цикл, пока часы не дойдут до нужной секунды
            while (Cosmos.HAL.RTC.Second != targetSecond)
            {
                // Небольшая ассемблерная заглушка, чтобы процессор не перегревался
                // (эквивалент пустого такта)
                sbyte a = 0;
            }
        }

        private void RenderGui()
        {
            try
            {
                // 1. Сплошной серый фон всего экрана
                canvas.Clear(Color.DarkGray);

                canvas.DrawString("Objects: " + allFSObjects.Count, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.Yellow), 500, 500);

                // Кнопка возврата на уровень вверх [^] возле пути
                canvas.DrawFilledRectangle(new Pen(Color.Gray), 20, 15, 35, 25);
                canvas.DrawString("[^]", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 28, 20);

                // Показ текущей директории сверху экрана
                canvas.DrawString($"Path: {guiCurrentDirectory}", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 70, 20);

                // Маленькая кнопка выхода [X] в самом углу
                canvas.DrawFilledRectangle(new Pen(Color.Red), 750, 15, 20, 20);
                canvas.DrawString("X", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 756, 18);

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
                        canvas.DrawFilledRectangle(new Pen(Color.Blue), 20, yOffset + 15, 350, 2);
                    }

                    // Вывод типа объекта и его имени
                    string prefix = allFSObjects[i].IsDirectory ? "[DIR]  " : "[FILE] ";
                    canvas.DrawString(prefix + allFSObjects[i].Name, Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 25, yOffset);
                    yOffset += 40;
                }

                // 3. СТРЕЛОЧКИ МНОГОСТРАНИЧНОСТИ СНИЗУ
                // Левая стрелка
                canvas.DrawFilledRectangle(new Pen(Color.Gray), 25, 480, 40, 30);
                canvas.DrawString("<-", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 37, 488);

                // Номер страницы
                int maxPage = Math.Max(1, (int)Math.Ceiling((double)allFSObjects.Count / FilesPerPage));
                canvas.DrawString($"{currentPage + 1} / {maxPage}", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 85, 488);

                // Right arrow
                canvas.DrawFilledRectangle(new Pen(Color.Gray), 150, 480, 40, 30);
                canvas.DrawString("->", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 162, 488);


                // 4. КНОПКИ УПРАВЛЕНИЯ (Справа)
                // Кнопка "Открыть / Войти" (Синяя)
                canvas.DrawFilledRectangle(new Pen(Color.FromArgb(52, 152, 219)), 500, 100, 140, 40);
                canvas.DrawString("Open / Enter", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 520, 112);

                // Кнопка "Удалить" (Красная)
                canvas.DrawFilledRectangle(new Pen(Color.FromArgb(231, 76, 60)), 500, 160, 140, 40);
                canvas.DrawString("Delete", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.White), 545, 172);


                // 5. ОБРАБОТКА КЛИКОВ МЫШИ
                int mX = (int)Sys.MouseManager.X;
                int mY = (int)Sys.MouseManager.Y;

                if (Sys.MouseManager.MouseState == Sys.MouseState.Left)
                {
                    if (!isMousePressed)
                    {
                        isMousePressed = true;

                        // Если блокнот открыт, блокируем клики по рабочему столу
                        if (!SamsaOS.GUI.NotepadGUI.isActive)
                        {
                            // Клик по [X] (Выход на рабочий стол)
                            if (mX >= 750 && mX <= 770 && mY >= 15 && mY <= 35)
                            {
                                isGuiActive = false;
                                canvas.Disable();
                                Console.Clear();
                                StartDesktop();
                                return;
                            }

                            // Клик по стрелочке возврата [^]
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

                            // Листание страниц стрелками <- и ->
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
                                        guiCurrentDirectory = guiCurrentDirectory + selectedObj.Name + @"\";
                                        selectedFileIndex = -1;
                                        currentPage = 0;
                                        UpdateFileList();
                                    }
                                    else
                                    {
                                        string fullPath = guiCurrentDirectory + selectedObj.Name;
                                        SamsaOS.GUI.NotepadGUI.Open(fullPath);
                                    }
                                }
                            }

                            // Действие: УДАЛИТЬ
                            if (mX >= 500 && mX <= 640 && mY >= 160 && mY <= 200)
                            {
                                if (selectedFileIndex != -1)
                                {
                                    int globalIdx = (currentPage * FilesPerPage) + selectedFileIndex;
                                    FSObject selectedObj = allFSObjects[globalIdx];
                                    string fullPath = guiCurrentDirectory + selectedObj.Name;
                                    try
                                    {
                                        if (selectedObj.IsDirectory) { Directory.Delete(fullPath, false); }
                                        else { File.Delete(fullPath); }
                                    }
                                    catch
                                    {
                                        canvas.DrawString("Error: Directory is not empty!", Cosmos.System.Graphics.Fonts.PCScreenFont.Default, new Pen(Color.Red), 500, 230);
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
                    // Сбрасываем клик, когда кнопка мыши отпущена
                    isMousePressed = false;
                }

                // ===== И ЗДЕСЬ ВСЁ ТОЧНО СНАРУЖИ =====

                // 1. Обновление анимаций
                SamsaOS.Animation2.Update();

                // 2. Отрисовка Блокнота
                SamsaOS.GUI.NotepadGUI.Render(canvas);

                // Отрисовка мыши "Уголком"
                canvas.DrawFilledRectangle(new Pen(Color.Black), mX, mY, 4, 12);
                canvas.DrawFilledRectangle(new Pen(Color.Black), mX, mY, 12, 4);
                canvas.DrawRectangle(new Pen(Color.White), mX, mY, 4, 12);
                canvas.DrawRectangle(new Pen(Color.White), mX, mY, 12, 4);

                // 4. ВЫВОД БУФЕРА НА ЭКРАН
                canvas.Display();

                // Очистка памяти
                Cosmos.Core.Memory.Heap.Collect();
                frameCounter++;
                if (frameCounter >= 300)
                {
                    Cosmos.Core.Memory.Heap.Collect();
                    frameCounter = 0;
                }

                for (int i = 0; i < 10000; i++) { sbyte a = 0; }
            }
            catch (Exception ex)
            {
                isGuiActive = false;
                canvas?.Disable();
                Console.WriteLine($"[GUI Crash]: {ex.Message}");
            }
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
