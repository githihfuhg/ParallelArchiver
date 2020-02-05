using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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

        await Task.Run(() =>
            {
                
                var pArh = new ParallelArchiver();
                pArh.ParallelArchiverEvents.Progress += EvenHandler;
                //pArh.CompressFile(path, $"{path}.mar");
                //pArh.CompressDirectory(path, $"{path}.mar");
                pArh.Decompress(path, Path.GetDirectoryName(path));


            });
            GC.Collect();
            timer.Stop();
            var time = timer.ElapsedMilliseconds;
            Console.WriteLine(time);
        }

        private static void EvenHandler(object sender, ProgressEventArgs e)
        {
            Console.WriteLine($"{e.FileName} - {e.CurrentFileProcent}%   Полный прогресс - {e.FullProgress}%");
        }
        
    }

}
