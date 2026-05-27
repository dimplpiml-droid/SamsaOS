using System;
using Cosmos.System.FileSystem;
using Sys = Cosmos.System;

namespace SamsaOS
{
    public class Kernel : Sys.Kernel
    {
        // Инициализация файловой системы Cosmos
        Sys.FileSystem.CosmosVFS vfs;
        CommandManager cmdManager;

        protected override void BeforeRun()
        {
            // Регистрируем виртуальную файловую систему (диск 0:\)
            vfs = new CosmosVFS();
            Sys.FileSystem.VFS.VFSManager.RegisterVFS(vfs);

            // Инициализируем обработчик команд
            cmdManager = new CommandManager();

            Console.Clear();

            // Приветственный звук 
            Console.Beep(440, 200);
            Console.Beep(554, 200); 
            Console.Beep(659, 300); 

            // Отрисовка названия ОС (Исправленный логотип SamsaOS)
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"  ____                               ___  ____  ");
            Console.WriteLine(@" / ___|  __ _ _ __ ___  ___  __ _   / _ \/ ___| ");
            Console.WriteLine(@" \___ \ / _` | '_ ` _ \/ __|/ _` | | | | \___ \ ");
            Console.WriteLine(@"  ___) | (_| | | | | | \__ \ (_| | | |_| |___) |");
            Console.WriteLine(@" |____/ \__,_|_| |_| |_|___/\__,_|  \___/|____/ ");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" Welcome to SamsaOS v0.1 Alpha!");
            Console.WriteLine(" Type 'help' for the list of commands.");
            Console.WriteLine("========================================");
        }

        protected override void Run()
        {
            Console.Write($"root@samsa:{cmdManager.CurrentDirectory}> ");

            string input = "";
            ConsoleKeyInfo key;

            while (true)
            {
                key = Console.ReadKey(true); // Читаем без вывода на экран

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.Tab)
                {
                    // вызываем автодополнение
                    string completion = cmdManager.AutoComplete(input);
                    if (!string.IsNullOrEmpty(completion))
                    {
                        // Очищаем текущий ввод и дописываем результат
                        for (int i = 0; i < input.Length; i++) Console.Write("\b \b");
                        input += completion;
                        Console.Write(input);
                    }
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                    {
                        input = input.Remove(input.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    input += key.KeyChar;
                    Console.Write(key.KeyChar);
                }
            }

            if (!string.IsNullOrWhiteSpace(input)) cmdManager.ProcessInput(input);
        }
    }
}