using System.Collections.Generic;
using System.IO.Compression;

namespace ArrArchiverLib.Compressor
{
    public class CompressorSettings
    {
        public int ThreadsCount { get; set; }
        public int ChunkSize  { get; set; }
        public string EncryptKey { get; set; }
        public CompressionLevel CompressionLevel { get; set; }
        public IEnumerable<string> TextFileExtensions { get; set; }
        public bool IsEncryptEnable => !string.IsNullOrEmpty(EncryptKey);
    }
}