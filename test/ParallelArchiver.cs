using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace test
{
    public static class ParallelArchiver
    {
        static public ParallelArchiverEvents ParallelArchiverEvents = new ParallelArchiverEvents();

        //static public CompressArchive compress = new CompressArchive();
        public static void CompressFile(string input, string result, PqzCompressionLevel compressL)
        {
            //var compressArch = new CompressArchive();
            //compressArch.CompressFile(input, result,compressL);
            //GC.Collect();
        }
        public static  async void CompressDirectory(string inputDir, string outputDir, PqzCompressionLevel compressL,Action<string, int, int> Progress = null)
        {
            var compressArch = new CompressArchive(/*ParallelArchiverEvents*/);
            var timer = new Stopwatch();
            timer.Start();
            if (Progress != null)
            { 
             
                     compressArch.Progress += Progress;

               
            }
            compressArch.CompressDirectory(inputDir,outputDir,compressL);
            timer.Stop();
            var time = timer.ElapsedMilliseconds;
            if(time > 0)
            {
                Console.WriteLine(time);
            }
            GC.Collect();
        }

        public static void Decompress(string inputFile, string outputDir, IEnumerable<string> fileExtension = null, IEnumerable<string> fileName = null)
        {
            var decompressArch = new DecompressArchive();
            decompressArch.Decompress(inputFile, outputDir, fileExtension, fileName);
            GC.Collect();
        }
        //private static void Notif(string fileName, int progressFile, int fullProgress)
        //{
        //    //Progress.Add($"{fileName} - {progressFile}% */  Полный прогресс - {fullProgress}%");
        //    //Console.WriteLine($"{fileName} - {progressFile}% */  Полный прогресс - {fullProgress}%");

        //    Console.WriteLine($"{fileName} - {progressFile}% */  Полный прогресс - {fullProgress}%");
        //    //Console.SetCursorPosition(0, 0);
        //}
        //public static void Decompress(string inputFile, string outputDir, IEnumerable<string> fileExtension)
        //{

        //}

        //public static void Decompress(string inputFile, string outputDir,IEnumerable<string> fileExtension, IEnumerable<string> fileName)
        //{

        //}
    }
}
