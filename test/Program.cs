using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
//using SharpCompress.

namespace test
{


    class Program
    {
        private static List<string> Progress = new List<string>();

        static void Main(string[] args)
        {
            var timer = new Stopwatch();
            timer.Start();
            //Console.ReadLine();
            Console.WriteLine("Введите путь в файлу для его архивации");
            Console.WriteLine(@"C:\Users\Win10Pro\Desktop\raar\test.mar");


            try
            {

                Async(Console.ReadLine());

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            timer.Stop();
            var time = timer.ElapsedMilliseconds;

            //Console.WriteLine(time);
            Console.ReadKey();




        }

        private static async void Async(string path)
        {
            var timer = new Stopwatch();
            timer.Start();
            //parallelGz.Progress += Notif;
            //ParallelArchiver.ParallelArchiverEvents.Progress += Notif;

            //ParallelArchiver.compress.Progress += Notif;


            await Task.Run(() =>
            {
                //parallelGz.CompressFile(path, $"{path}.gz", PqzCompressionLevel.Optimal);
                //parallelGz.CreateTar($"{Path}.tar", Path);
                //parallelGz.CompressDirectory(path, $"{path}.mar", PqzCompressionLevel.Optimal);
                //parallelGz.Decompress(path, path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar)));
                //ParallelArchiver.CompressFile(path, $"{path}.mar", PqzCompressionLevel.Optimal, EventHandler);
                //ParallelArchiver.CompressDirectory(path, $"{path}.mar", PqzCompressionLevel.Optimal/*, EventHandler*/);
                ParallelArchiver.Decompress(path, path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar)),/*new[] { ".pdf" },*/  progressHandler: EventHandler);

            });
            GC.Collect();
            timer.Stop();
            var time = timer.ElapsedMilliseconds;
            Console.WriteLine(time);
        }

        private static async void EventHandler(string fileName, int progressFile, int fullProgress)
        {
           // await Task.Run(() =>
           //{

               Console.WriteLine($"{fileName} - {progressFile}% */  Полный прогресс - {fullProgress}%");

           //});
            //Progress.Add($"{fileName} - {progressFile}% */  Полный прогресс - {fullProgress}%");
            //Console.WriteLine($"{fileName} - {progressFile}% */  Полный прогресс - {fullProgress}%");
        }


        public static void Text()
        {

            string a = "";

            File.WriteAllText(@"C:\Users\Ярослав\Desktop\Extension.txt",
                Regex.Matches(File.ReadAllText(@"C:\Users\Ярослав\Desktop\123.txt"), @"\.\w+\s")
                    .Select(txt => a += $"\"{txt.Value}\",").ToArray()[^1].Replace(" ",""));

            
        }
    }

}
