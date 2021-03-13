using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArrArchiverLib.Archiver;
using ArrArchiverLib.Progress;

namespace ArchiverTest
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            var archiver = new Archiver();

            archiver.Progress.ProgressChanged += ProgressHandler;
            
            await CreateMenu(archiver);
        }

        private static async Task CreateMenu(Archiver archiver)
        {
            while (true)
            {
                Console.WriteLine("1) Создать архив");
                Console.WriteLine("2) Распаковать архив");
                Console.WriteLine("3) Выход");

                var input = Console.ReadLine();

                Console.Clear();

                try
                {
                    switch (input)
                    {
                        case "1":
                            await CreateArchive(archiver);
                            break;
                        case "2":
                            await ExtractArchive(archiver);
                            break;
                        case "3":
                            return;
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.ReadLine();
                    Console.Clear();
                }
                archiver.Compressor.Settings.EncryptKey = null;
                archiver.Decompressor.Settings.EncryptKey = null;
                Console.ReadLine();
                Console.Clear();
            }
        }

        private static void ProgressHandler(object sender, ArchiveProgressEventArgs eventArgs)
        {
            Console.WriteLine($"Name: {eventArgs.FileName}\n" +
                              $"AllProgress: {eventArgs.AllProgress}%\n" +
                              $"CurrentFileProgress: {eventArgs.CurrentFileProgress}%\n" +
                              $"Time: {eventArgs.ElapsedMilliseconds} ms");
        }

        private static async Task CreateArchive(Archiver archiver)
        {
            var path = GetPath("Введите путь к файлам");
                        
            Console.WriteLine("Использовать шифрование(Да/Нет)?");
                        
            var result = Console.ReadLine() ?? "";
            var isUseEncrypt = result.Equals("Да", StringComparison.CurrentCultureIgnoreCase);

            if (isUseEncrypt)
            {
                Console.WriteLine("Введите ключ");
                var key = Console.ReadLine();
                archiver.Compressor.Settings.EncryptKey = key;
            }
            await archiver.CreateAsync(path, $"{path}.arh");
            
        }
        
        private static async Task ExtractArchive(Archiver archiver)
        {
            var path = GetPath("Введите путь к архиву");
            var archiveInfo = await archiver.GetArchiveInfoAsync(path);

            if (archiveInfo.IsEncrypted)
            {
                Console.WriteLine("Архив зашифрован, введите ключ");
                var key = Console.ReadLine();
                archiver.Decompressor.Settings.EncryptKey = key;
            }
                        
            await archiver.ExtractAsync(path, Path.GetDirectoryName(path));
        }

        private static string GetPath(string message)
        {
            Console.WriteLine(message);
            return Console.ReadLine();
        }
    }
}