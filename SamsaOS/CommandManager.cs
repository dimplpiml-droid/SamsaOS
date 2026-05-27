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
            // Разбиваем строку на части (по пробелу)
            string[] split = input.Split(' ');
            string command = split[0].ToLower(); // Сама команда (например, cd)

            // Собираем аргументы, если они есть
            string[] args = new string[split.Length - 1];
            Array.Copy(split, 1, args, 0, split.Length - 1);

            try
            {
                ExecuteCommand(command, args);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                // Сообщение об ошибке на английском
                Console.WriteLine($"[Execution Error]: {ex.Message}");
                Console.ResetColor();
            }
        }

        private void ExecuteCommand(string command, string[] args)
        {
            switch (command)
            {
                // ==========================================
                // 1. СИСТЕМНЫЕ КОМАНДЫ (БАЗА)
                // ==========================================
                case "help":
                    Console.WriteLine("Available commands:");
                    Console.WriteLine(" System: help, about, clear, time, shutdown, reboot");
                    Console.WriteLine(" Files:  ls, cd, mkdir, touch, rm, cat, echo");
                    Console.WriteLine(" Apps:   miv, calc, matrix");
                    Console.WriteLine(" Debug:  sysinfo, gc, crash");
                    break;

                case "about":
                    Console.WriteLine("SamsaOS - An OS built on Cosmos Kernel.");
                    Console.WriteLine("Developers: Samat and Alexey");
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

                // ==========================================
                // 2. КОМАНДЫ ДЛЯ РАБОТЫ С ФАЙЛАМИ (ФС)
                // ==========================================
                case "ls":
                case "dir":
                    var dirs = Directory.GetDirectories(CurrentDirectory);
                    var files = Directory.GetFiles(CurrentDirectory);

                    Console.ForegroundColor = ConsoleColor.Blue;
                    foreach (var dir in dirs) Console.WriteLine($"[DIR]  {dir}");
                    Console.ForegroundColor = ConsoleColor.White;
                    foreach (var file in files) Console.WriteLine($"[FILE] {file}");
                    break;

                case "cd":
                    if (args.Length == 0) { Console.WriteLine("Specify a directory!"); break; }

                    if (args[0] == "..")
                    {
                        // Логика возврата назад
                        if (CurrentDirectory != @"0:\")
                        {
                            // Отрезаем последнюю часть пути
                            string parent = CurrentDirectory.Substring(0, CurrentDirectory.Length - 1);
                            parent = parent.Substring(0, parent.LastIndexOf(@"\") + 1);
                            CurrentDirectory = parent;
                        }
                    }
                    else
                    {
                        // Логика входа в папку
                        string target = CurrentDirectory + args[0] + @"\";
                        if (Directory.Exists(target)) CurrentDirectory = target;
                        else Console.WriteLine("Directory not found.");
                    }
                    break;

                case "mkdir":
                    if (args.Length == 0) { Console.WriteLine("Specify directory name."); break; }
                    Directory.CreateDirectory(CurrentDirectory + args[0]);
                    Console.WriteLine($"Directory {args[0]} created.");
                    break;

                case "touch":
                    if (args.Length == 0) { Console.WriteLine("Specify file name."); break; }
                    File.Create(CurrentDirectory + args[0]).Close();
                    Console.WriteLine($"File {args[0]} created.");
                    break;

                case "rm":
                    if (args.Length == 0) { Console.WriteLine("Specify file/directory name."); break; }
                    string rmPath = CurrentDirectory + args[0];
                    if (File.Exists(rmPath)) { File.Delete(rmPath); Console.WriteLine("File deleted."); }
                    else if (Directory.Exists(rmPath)) { Directory.Delete(rmPath, true); Console.WriteLine("Directory deleted."); }
                    else Console.WriteLine("Object not found.");
                    break;

                case "cat":
                    if (args.Length == 0) { Console.WriteLine("Specify a file."); break; }
                    string catPath = CurrentDirectory + args[0];
                    if (File.Exists(catPath)) Console.WriteLine(File.ReadAllText(catPath));
                    else Console.WriteLine("File not found.");
                    break;

                case "echo":
                    if (args.Length < 2) { Console.WriteLine("Usage: echo [text] [file]"); break; }
                    string text = string.Join(" ", args, 0, args.Length - 1);
                    string echoFile = CurrentDirectory + args[args.Length - 1];
                    File.WriteAllText(echoFile, text);
                    Console.WriteLine("Text written to file.");
                    break;

                // ==========================================
                // 3. УТИЛИТЫ И ОТЛАДКА (ЗАГЛУШКИ)
                // ==========================================
                //case "gc":
                //    long before = GC.GetAvailableMBytes();
                //    GC.Collect();
                //    long after = GC.GetAvailableMBytes();
                //    Console.WriteLine($"Garbage collection finished. Memory: {before}MB -> {after}MB");
                //    break;

                //case "sysinfo":
                //    Console.WriteLine($"Free memory: {GC.GetAvailableMBytes()} MB");
                //    Console.WriteLine("CPU: x86/x64 Compatible");
                //    Console.WriteLine($"Disk 0: {new DriveInfo(@"0:\").TotalSize / 1024 / 1024} MB");
                //    break;

                //case "crash":
                //    throw new Exception("Manual critical system error triggered!");

                //case "miv":
                //    Console.WriteLine("Launching miv... (Text editor code goes here)");
                //    break;

                //case "calc":
                //    Console.WriteLine("Launching calc... (Calculator code goes here)");
                //    break;

                //case "matrix":
                //    Console.WriteLine("Wake up, Neo... (Matrix code goes here)");
                //    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Command '{command}' not found. Type 'help'.");
                    Console.ResetColor();
                    return;
                    break;
            }
        }
    }
}