using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace test
{
    public class ParallelArchiver
    {
        public ParallelArchiverEvents ParallelArchiverEvents { get; }
        private DecompressArchive DecompressArchive { get; set; }
        private CompressArchive CompressArchive { get; set; }
        public PqzCompressionLevel CompressLevel { get; set; }
        public bool MaximumTxtCompression { get; set; }
        
        public ParallelArchiver()
        {
            ParallelArchiverEvents = new ParallelArchiverEvents();
            DecompressArchive = new DecompressArchive(ParallelArchiverEvents);
            CompressLevel = PqzCompressionLevel.Optimal;
            MaximumTxtCompression = false;
        }
        public void CompressFile(string input, string result)
        {
            CompressArchive = new CompressArchive(ParallelArchiverEvents, CompressLevel, MaximumTxtCompression);
            CompressArchive.CompressFile(input, result);
            GC.Collect();
        }
        public async void CompressFileAsync(string input, string result)
        {
            await Task.Run(() =>
            {
                CompressArchive = new CompressArchive(ParallelArchiverEvents, CompressLevel, MaximumTxtCompression);
                CompressArchive.CompressFile(input, result);
            });
            GC.Collect();
        }

        public void CompressDirectory(string inputDir, string outputDir)
        {
            //var compressArch = new CompressArchive();
            CompressArchive = new CompressArchive(ParallelArchiverEvents, CompressLevel, MaximumTxtCompression);
            CompressArchive.CompressDirectory(inputDir, outputDir);
            GC.Collect();
        }
        public async void CompressDirectoryAsync(string inputDir, string outputDir)
        {
            await Task.Run(() =>
            {
                CompressArchive = new CompressArchive(ParallelArchiverEvents, CompressLevel, MaximumTxtCompression);
                CompressArchive.CompressDirectory(inputDir, outputDir);
            });
            GC.Collect();
        }
        public void Decompress(string inputFile, string outputDir, IEnumerable<string> fileExtension = null, IEnumerable<string> fileName = null)
        {
            DecompressArchive.Decompress(inputFile, outputDir, fileExtension, fileName);
            GC.Collect();
        }
        public async void DecompressAsync(string inputFile, string outputDir, IEnumerable<string> fileExtension = null, IEnumerable<string> fileName = null)
        {
            await Task.Run(() =>
            {
                DecompressArchive.Decompress(inputFile, outputDir, fileExtension, fileName);
            });
            GC.Collect();
        }
        public string[] GetFile(string path)
        {
            return DecompressArchive.GetFiles(path);
        }

    }
}
