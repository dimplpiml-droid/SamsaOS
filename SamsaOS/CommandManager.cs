using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.Generic;

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
                Console.WriteLine($"[Error]: {ex.Message}");
            }
        }

        private void ExecuteCommand(string command, string[] args)
        {
            switch (command)
            {

                // 1

                case "help":
                    Console.WriteLine("Available commands:");
                    Console.WriteLine(" System: help, about, clear, time, shutdown, reboot");
                    Console.WriteLine(" Files:  ls, cd, mkdir, touch, rm, cat, echo");
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