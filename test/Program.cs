using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
//using SharpCompress.

namespace test
{
   

    class Program
    {
        private static List <string > Progress = new List<string>();

        static void Main(string[] args)
        {
            var timer = new Stopwatch();
            timer.Start();
            //Console.ReadLine();
            Console.WriteLine("Введите путь в файлу для его архивации");
            Console.WriteLine(@"C:\Users\Win10Pro\Desktop\raar\test.mar");
            var parallelGz = new ParallelGz();


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
            var parallelGz = new ParallelGz();
            parallelGz.Progress += Notif;
            await Task.Run(() =>
            {
                //parallelGz.CompressFile(path, $"{path}.gz", PqzCompressionLevel.Optimal);
                //parallelGz.CreateTar($"{Path}.tar", Path);
                //parallelGz.CompressDirectory(path, $"{path}.mar", PqzCompressionLevel.Optimal);
                parallelGz.Decompress(path, path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar)));
            });
            GC.Collect();
            timer.Stop();
            var time = timer.ElapsedMilliseconds;
            Console.WriteLine(time);
        }

        private static void Notif(string fileName,int progressFile,int fullProgress)
        {
            //Progress.Add($"{fileName} - {progressFile}% */  Полный прогресс - {fullProgress}%");
            //Console.WriteLine($"{fileName} - {progressFile}% */  Полный прогресс - {fullProgress}%");

            //Console.Write($"{fileName} - {progressFile}% */  Полный прогресс - {fullProgress}%");
            //Console.SetCursorPosition(0, 0);
        }

    }

    

}
