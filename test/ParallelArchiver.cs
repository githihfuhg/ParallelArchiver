using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace test
{
    public static class ParallelArchiver
    {
        //static public ParallelArchiverEvents ParallelArchiverEvents = new ParallelArchiverEvents();

        //static public CompressArchive compress = new CompressArchive();
        public static void CompressFile(string input, string result, PqzCompressionLevel compressL, Action<string, int, int> progressHandler = null)
        {
            var compressArch = new CompressArchive();
            compressArch.Progress += progressHandler;
            compressArch.CompressFile(input, result, compressL);
            compressArch.Progress -= progressHandler;
            GC.Collect();
        }
        public static async void CompressFileAsync(string input, string result, PqzCompressionLevel compressL, Action<string, int, int> progressHandler = null)
        {
            await Task.Run(() =>
            {
                var compressArch = new CompressArchive();
                compressArch.Progress += progressHandler;
                compressArch.CompressFile(input, result, compressL);
                compressArch.Progress -= progressHandler;

            });
            GC.Collect();
        }

        public static  void CompressDirectory(string inputDir, string outputDir, PqzCompressionLevel compressL, Action<string, int, int> progressHandler = null)
        {
            var compressArch = new CompressArchive();
            compressArch.Progress += progressHandler;
            compressArch.CompressDirectory(inputDir, outputDir, compressL);
            compressArch.Progress -= progressHandler;
            GC.Collect();
        }
        public static async void CompressDirectoryAsync(string inputDir, string outputDir, PqzCompressionLevel compressL, Action<string, int, int> progressHandler = null)
        {
            await Task.Run(() =>
            {
                var compressArch = new CompressArchive();
                compressArch.Progress += progressHandler;
                compressArch.CompressDirectory(inputDir, outputDir, compressL);
                compressArch.Progress -= progressHandler;
            });
            GC.Collect();
        }

        public static void Decompress(string inputFile, string outputDir, IEnumerable<string> fileExtension = null, IEnumerable<string> fileName = null, Action<string, int, int> progressHandler = null)
        {
            var decompressArch = new DecompressArchive();
            decompressArch.Progress += progressHandler;
            decompressArch.Decompress(inputFile, outputDir, fileExtension, fileName);
            decompressArch.Progress -= progressHandler;
            GC.Collect();
        }
        public static async void DecompressAsync(string inputFile, string outputDir, IEnumerable<string> fileExtension = null, IEnumerable<string> fileName = null, Action<string, int, int> progressHandler = null)
        {
            await Task.Run(() =>
            {
                var decompressArch = new DecompressArchive();
                decompressArch.Progress += progressHandler;
                decompressArch.Decompress(inputFile, outputDir, fileExtension, fileName);
                decompressArch.Progress -= progressHandler;
            });
            GC.Collect();
        }

    }
}
