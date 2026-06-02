using System;
using System.Collections.Generic;
using System.IO;

namespace SamsaOS
{
    public class CommandManager
    {

        public string CurrentDirectory { get; private set; } = @"0:\";

        public void ProcessInput(string input)
        {
            //кавычкм
            List<string> parts = new List<string>();
            bool inQuotes = false;
            string current = "";

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\"') inQuotes = !inQuotes;
                else if (input[i] == ' ' && !inQuotes)
                {
                    if (current != "") parts.Add(current);
                    current = "";
                }
                else current += input[i];
            }
            if (current != "") parts.Add(current);

            if (parts.Count == 0) return;

            string command = parts[0].ToLower();
            string[] args = parts.GetRange(1, parts.Count - 1).ToArray();

            try { ExecuteCommand(command, args); }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error]: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private void ExecuteCommand(string command, string[] args)
        {

            switch (command)
            {
                case "diskinfo":
                    try
                    {
                        // Проверяем, существует ли корневой каталог
                        if (Directory.Exists(@"0:\"))
                        {
                            Console.WriteLine("Root directory 0:\\ exists and is accessible!");
                            var availableSpace = Directory.GetFiles(@"0:\");
                            Console.WriteLine($"Files in root: {availableSpace.Length}");
                        }
                        else
                        {
                            Console.WriteLine("Error: Drive 0:\\ is NOT accessible or not formatted!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Disk test failed: {ex.GetType().Name} - {ex.Message}");
                    }
                    break;

                // 1

                case "start":
                    Console.WriteLine("Запуск графического интерфейса SamsaOS...");
                    Kernel.StartGuiMode(CurrentDirectory);
                    break;

                case "desktop":
                    Console.WriteLine("Launching desktop...");
                    Kernel.StartDesktop();
                    break;

                case "help":
                    Console.WriteLine("Press TAB for autocomplete file or dir name (not command)");
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("GUI: start, desktop"); Console.WriteLine(" System: help, about, clear, time, shutdown, reboot");
                    Console.WriteLine(" Files:  ");
                    Console.WriteLine(" ls / dir        - show all files and folders in current directory");
                    Console.WriteLine(" cd [dirname]    - change current directory");
                    Console.WriteLine(" pwd             - print current path");
                    Console.WriteLine(" mkdir [name]    - create new directory");
                    Console.WriteLine(" touch [name] - create a new file");
                    Console.WriteLine(" echo \"text\" [f] - rewrite text ");
                    Console.WriteLine(" miv [file]      - edit text");
                    Console.WriteLine(" cat [file]      - read text");
                    Console.WriteLine(" rm [file]       - delete file");
                    Console.WriteLine(" rmdir [dirname] - delete EMPTY directory");
                    Console.WriteLine(" diskinfo - information about disk");
                    Console.WriteLine();
                    Console.WriteLine(" Apps:   miv, calc, matrix");
                    Console.WriteLine(" Debug:  sysinfo, gc, crash");
                    Console.WriteLine(" Game:   snake");
                    break;

                case "about":
                    Console.WriteLine("SamsaOS - An OS built on Cosmos Kernel.");
                    Console.WriteLine("Developers team: Samat, Alexey , Daniil, Ilyas");
                    Console.WriteLine("Created with C#.");
                    break;

                case "clear":
                case "cls":
                    Console.Clear();
                    break;

                case "time":
                    Console.WriteLine($"Current time: {Cosmos.HAL.RTC.Hour}:{Cosmos.HAL.RTC.Minute}:{Cosmos.HAL.RTC.Second}");
                    break;

                case "shutdown":
                    Console.WriteLine("Shutting down SamsaOS...");
                    Cosmos.System.Power.Shutdown();
                    break;

                case "reboot":
                    Console.WriteLine("Rebooting...");
                    Cosmos.System.Power.Reboot();
                    break;


                // 2

                case "ls":
                case "dir":
                    try
                    {
                        string[] dirs = Directory.GetDirectories(CurrentDirectory);
                        string[] files = Directory.GetFiles(CurrentDirectory);

                        if (dirs.Length == 0 && files.Length == 0)
                        {
                            Console.WriteLine("Directory is empty.");
                            break;
                        }

                        Console.ForegroundColor = ConsoleColor.Blue;
                        foreach (var dir in dirs)
                        {
                            // Убираем полный путь, оставляя только имя папки
                            string dirName = dir.Replace(CurrentDirectory, "").Replace(@"\", "");
                            Console.WriteLine($"[DIR]  {dirName}");
                        }

                        Console.ForegroundColor = ConsoleColor.White;
                        foreach (var file in files)
                        {
                            // Оставляем только имя файла
                            string fileName = file.Replace(CurrentDirectory, "");
                            Console.WriteLine($"[FILE] {fileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error]: Cannot read directory. {ex.Message}");
                    }
                    break;

                case "cd":
                    if (args.Length == 0)
                    {
                        Console.WriteLine("Specify a directory! Usage: cd [dirname] or cd ..");
                        break;
                    }

                    string targetDir = args[0];

                    if (targetDir == "..")
                    {
                        // Если мы уже в корне, возвращаться некуда
                        if (CurrentDirectory == @"0:\")
                        {
                            Console.WriteLine("Already in root directory.");
                        }
                        else
                        {
                            // Отрезаем последний слеш, ищем предыдущий и обрезаем строку
                            string temp = CurrentDirectory.Substring(0, CurrentDirectory.Length - 1);
                            CurrentDirectory = temp.Substring(0, temp.LastIndexOf(@"\") + 1);
                        }
                    }
                    else
                    {
                        // Формируем путь к целевой папке
                        string pathToCheck = CurrentDirectory + targetDir + @"\";

                        if (Directory.Exists(pathToCheck))
                        {
                            CurrentDirectory = pathToCheck;
                        }
                        else
                        {
                            Console.WriteLine($"Error: Directory '{targetDir}' not found.");
                        }
                    }
                    break;

                case "mkdir":
                    if (args.Length == 0) { Console.WriteLine("Specify directory name."); break; }

                    string newDirName = args[0];
                    string cleanPath = CurrentDirectory.TrimEnd('\\');
                    string newFolderPath = cleanPath + @"\" + newDirName;

                    try
                    {
                        if (!Directory.Exists(newFolderPath))
                        {
                            Directory.CreateDirectory(newFolderPath);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Directory '{newDirName}' created.");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.WriteLine("Error: Directory already exists.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error]: {ex.Message}");
                    }
                    break;

                case "echo":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: echo \"your text\" [filename]");
                        break;
                    }

                    string targetFile = args[args.Length - 1];

                    // Безопасное формирование пути в подпапке для Cosmos VFS
                    string cleanDirectory = CurrentDirectory.TrimEnd('\\');
                    string echoFilePath = cleanDirectory + @"\" + targetFile;

                    string content = "";
                    if (args.Length == 2) content = args[0];
                    else content = string.Join(" ", args, 0, args.Length - 1);

                    try
                    {
                        File.WriteAllText(echoFilePath, content);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Text successfully written to '{targetFile}'.");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error]: {ex.Message}");
                    }
                    break;


                case "touch":
                    if (args.Length == 0) { Console.WriteLine("Specify file name."); break; }

                    string touchFileName = args[0];

                    if (!touchFileName.Contains(".") && touchFileName.Length > 8)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Warning: Name is too long for FAT32 (max 8 chars).");
                        touchFileName = touchFileName.Substring(0, 8);
                        Console.WriteLine($"File will be created as: '{touchFileName}'");
                        Console.ResetColor();
                    }

                    string cleanedDir = CurrentDirectory.TrimEnd('\\');
                    string touchPath = cleanedDir + @"\" + touchFileName;

                    // ПРОВЕРКА НА СУЩЕСТВОВАНИЕ ФАЙЛА
                    if (File.Exists(touchPath))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: File '{touchFileName}' already exists. Operation aborted.");
                        Console.ResetColor();
                        break; // Прерываем выполнение команды, файл не перезапишется
                    }

                    try
                    {
                        File.WriteAllText(touchPath, string.Empty);
                        Console.WriteLine($"File '{touchFileName}' created successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[VFS Error]: {ex.Message}");
                    }
                    break;

                case "rm":
                    if (args.Length == 0) { Console.WriteLine("Specify file/directory name."); break; }
                    string rmPath = CurrentDirectory + args[0];
                    if (File.Exists(rmPath)) { File.Delete(rmPath); Console.WriteLine("File deleted."); }
                    else if (Directory.Exists(rmPath)) { Directory.Delete(rmPath, true); Console.WriteLine("Directory deleted."); }
                    else Console.WriteLine("Object not found.");
                    break;

                case "cat":
                    if (args.Length == 0) { Console.WriteLine("Specify file name. Usage: cat [filename]"); break; }

                    string fileToRead = args[0];
                    string catFilePath = CurrentDirectory + fileToRead;

                    if (File.Exists(catFilePath))
                    {
                        try
                        {
                            string[] lines = File.ReadAllLines(catFilePath);
                            foreach (var line in lines)
                            {
                                Console.WriteLine(line);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Error reading file]: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: File '{fileToRead}' not found.");
                    }
                    break;



                case "pwd":
                    Console.WriteLine($"Current path: {CurrentDirectory}");
                    break;




                // 3

                case "gc":
                    Cosmos.Core.Memory.Heap.Collect();
                    Console.WriteLine("Garbage collection finished.");
                    break;

                case "sysinfo":
                    Console.ForegroundColor = ConsoleColor.Cyan;

                    Console.WriteLine($"CPU: {Cosmos.Core.CPU.GetCPUBrandString()}");
                    Console.WriteLine($"RAM: {Cosmos.Core.CPU.GetAmountOfRAM()} MB");
                    Console.ResetColor();
                    break;

                case "crash":

                    throw new Exception("Manual critical system error triggered by user!");


                // 4

                case "miv":
                    if (args.Length == 0) { Console.WriteLine("Usage: miv [filename]"); break; }
                    SamsaOS.Apps.MivEditor.Run(args[0]);
                    break;

                case "calc":
                    SamsaOS.Apps.CalculatorApp.Run();
                    Console.WriteLine("calc is under construction. Coming soon!");
                    break;

                case "matrix":
                    SamsaOS.Apps.MatrixApp.Run();
                    break;

                case "snake":
                    SamsaOS.Apps.SnakeGame.Run();
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Command '{command}' not found. Type 'help'.");
                    Console.ResetColor();
                    return;
                    break;
            }

        }

        public string AutoComplete(string currentInput)
        {
            // Ищем файлы и папки в текущей директории
            var files = Directory.GetFiles(CurrentDirectory);
            var dirs = Directory.GetDirectories(CurrentDirectory);

            List<string> candidates = new List<string>();
            candidates.AddRange(files);
            candidates.AddRange(dirs);

            foreach (var candidate in candidates)
            {
                // Получаем только имя (убираем путь)
                string name = Path.GetFileName(candidate);

                // Если имя файла начинается с того, что уже ввел юзер
                if (name.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase))
                {
                    // Возвращаем часть, которой не хватает
                    return name.Substring(currentInput.Length);
                }
            }
            return null;
        }
    }
}