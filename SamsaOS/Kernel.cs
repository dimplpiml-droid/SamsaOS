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
            // Выводим приглашение ко вводу с текущей директорией
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"@samsa:{cmdManager.CurrentDirectory}> ");
            Console.ResetColor();

            // Читаем ввод пользователя
            var input = Console.ReadLine();

            // Передаем строку в CommandManager на обработку
            if (!string.IsNullOrWhiteSpace(input))
            {
                cmdManager.ProcessInput(input);
            }
        }
    }
}