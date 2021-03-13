using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArrArchiverLib.Compressor;

namespace ArrArchiverLib.Metadata.Models
{
    public class FileHeader
    {
        public bool IsEncrypted { get; set; }
        public CompressionType CompressionType { get; set; }
        public int RelativePathLength { get; set; }
        public string RelativePath { get; set; }
        public string FullPath { get; set; } //Skipped
        public long Position { get; set; }
        public int NumberOfChunks { get; set; }
        public List<ChunkHeader> Chunks { get; set; } = new();
        public long FileSize { get; set; } //Skipped
        
        public int SizeOf //Skipped
        {
            get
            {
                var relativePathSize = Encoding.UTF8.GetBytes(RelativePath).Length;
                var chunksSize = 8 * NumberOfChunks;

                return sizeof(int) + sizeof(int) + relativePathSize + sizeof(long) + sizeof(int) + chunksSize + sizeof(bool);
            }
            
        }
    }
}