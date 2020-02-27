using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ParallelZip
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
            CompressLevel = PqzCompressionLevel.Optimal;
            MaximumTxtCompression = false;
        }
        public void CompressFile(string input, string result)
        {
            CompressArchive = new CompressArchive(ParallelArchiverEvents, CompressLevel, MaximumTxtCompression);
            CompressArchive.CompressFile(input, result);
            GC.Collect();
        }
        public Task CompressFileAsync(string input, string result)
        {
            return Task.Run(() =>
            {
                CompressArchive = new CompressArchive(ParallelArchiverEvents, CompressLevel, MaximumTxtCompression);
                CompressArchive.CompressFile(input, result);
                GC.Collect();
            });
        }

        public void CompressDirectory(string inputDir, string outputDir)
        {
            CompressArchive = new CompressArchive(ParallelArchiverEvents, CompressLevel, MaximumTxtCompression);
            CompressArchive.CompressDirectory(inputDir, outputDir);
            GC.Collect();
        }
        public Task CompressDirectoryAsync(string inputDir, string outputDir)
        {
            return Task.Run(() =>
            {
                CompressArchive = new CompressArchive(ParallelArchiverEvents, CompressLevel, MaximumTxtCompression);
                CompressArchive.CompressDirectory(inputDir, outputDir);
                GC.Collect();
            });

        }
        public void Decompress(string inputFile, string outputDir, IEnumerable<string> fileExtension = null, IEnumerable<string> fileName = null)
        {
            DecompressArchive = new DecompressArchive(ParallelArchiverEvents);
            DecompressArchive.Decompress(inputFile, outputDir, fileExtension, fileName);
            GC.Collect();
        }
        public Task DecompressAsync(string inputFile, string outputDir, IEnumerable<string> fileExtension = null, IEnumerable<string> fileName = null)
        {
            return Task.Run(() =>
            {
                DecompressArchive = new DecompressArchive(ParallelArchiverEvents);
                DecompressArchive.Decompress(inputFile, outputDir, fileExtension, fileName);
                GC.Collect();
            });

        }
        public string[] GetFile(string path)
        {
            DecompressArchive = new DecompressArchive(ParallelArchiverEvents);
            return DecompressArchive.GetFiles(path);
        }

    }
}
