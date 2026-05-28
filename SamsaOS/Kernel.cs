using System;
using Cosmos.System.FileSystem;
using Sys = Cosmos.System;

namespace SamsaOS
{
    public class Kernel : Sys.Kernel
    {
        // инициализация файловой системы
        Sys.FileSystem.CosmosVFS vfs;
        CommandManager cmdManager;

        protected override void BeforeRun()
        {
            // Регистрируем виртуальную файловую систему 
            vfs = new CosmosVFS();
            Sys.FileSystem.VFS.VFSManager.RegisterVFS(vfs);

            // Инициализируем обработчик команд
            cmdManager = new CommandManager();

            Console.Clear();


            Console.Beep(440, 200);
            Console.Beep(554, 200); 
            Console.Beep(659, 300); 


            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"  ____                               ___  ____  ");
            Console.WriteLine(@" / ___|  __ _ _ __ ___  ___  __ _   / _ \/ ___| ");
            Console.WriteLine(@" \___ \ / _` | '_ ` _ \/ __|/ _` | | | | \___ \ ");
            Console.WriteLine(@"  ___) | (_| | | | | | \__ \ (_| | | |_| |___) |");
            Console.WriteLine(@" |____/ \__,_|_| |_| |_|___/\__,_|  \___/|____/ ");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" Welcome to SamsaOS v0.1!");
            Console.WriteLine(" Type 'help' for the list of commands.");
            Console.WriteLine("========================================");
        }

        protected override void Run()
        {

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