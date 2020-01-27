using System;
using System.Collections.Generic;
using System.Text;

namespace test
{
    public static class ParallelArchiver
    {
        public static void CompressFile(string input, string result, PqzCompressionLevel compressL)
        {
            var compressArch = new CompressArchive();
            compressArch.CompressFile(input, result,compressL);
            GC.Collect();
        }

        public static void CompressDirectory(string inputDir, string outputDir, PqzCompressionLevel compressL)
        {
            var compressArch = new CompressArchive();
            compressArch.CompressFile(inputDir,outputDir,compressL);
            GC.Collect();
        }

        public static void Decompress(string inputFile, string outputDir, IEnumerable<string> fileExtension = null, IEnumerable<string> fileName = null)
        {
            var decompressArch = new DecompressArchive();
            decompressArch.Decompress(inputFile, outputDir, fileExtension, fileName);
            GC.Collect();
        }

        //public static void Decompress(string inputFile, string outputDir, IEnumerable<string> fileExtension)
        //{

        //}

        //public static void Decompress(string inputFile, string outputDir,IEnumerable<string> fileExtension, IEnumerable<string> fileName)
        //{

        //}
    }
}
