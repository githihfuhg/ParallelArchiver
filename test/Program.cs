using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//using SharpCompress.

namespace test
{


    class Program
    {
        static void Main(string[] args)
        {
            var pArh = new ParallelArchiver();
            string path = "";

            try
            {
                while (true)
                {

                    Console.Clear();
                    Console.WriteLine("Выводить прогресс ? (да/нет)");
                    if (Console.ReadLine() == "да")
                    {
                        pArh.ParallelArchiverEvents.Progress += EvenHandler;
                    }
                    else
                    {
                        pArh.ParallelArchiverEvents.Progress -= EvenHandler;
                    }

                    Console.Clear();
                    Console.WriteLine("1.Aрхивировать файл");
                    Console.WriteLine("2.Aрхивировать директорию");
                    Console.WriteLine("3.Распаковать архив");

                    switch (Console.ReadLine())
                    {
                        case "1":
                        {
                            Console.Clear();
                            Console.WriteLine("1.Введите путь к файлу");
                            path = Console.ReadLine();
                            Timer(() => pArh.CompressFile(path, $"{path}.mar"));
                            break;
                        }
                        case "2":
                        {
                            Console.Clear();
                            Console.WriteLine("1.Введите путь к директории");
                            path = Console.ReadLine();
                            Timer(() => pArh.CompressDirectory(path, $"{path}.mar"));
                            break;
                        }
                        case "3":
                        {
                            Console.Clear();
                            Console.WriteLine("1.Введите путь к архиву");
                            path = Console.ReadLine();
                            Timer(() => pArh.Decompress(path, Path.GetDirectoryName(path)));
                            break;
                        }
                        default:
                        {
                            Console.Clear();
                            Console.WriteLine("Данные введены некоректно!!");
                            break;
                        }
                        case "exit":
                        {
                            return;
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadKey();
        }

        private static void Timer(/*string path,string type*//*Action<string,string,string> action*/Action action)
        {
            var timer = new Stopwatch();
            timer.Start();
            action();
            timer.Stop();
            var time = timer.ElapsedMilliseconds;
            Console.WriteLine($"Время выполнения {time} мс");
            Console.ReadKey();
        }

        private static void EvenHandler(object sender, ProgressEventArgs e)
        {
            Console.WriteLine($"{e.FileName} - {e.CurrentFileProcent}%   Полный прогресс - {e.FullProgress}%");
        }

    }

}
