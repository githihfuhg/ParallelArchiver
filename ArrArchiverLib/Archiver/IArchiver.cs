using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArrArchiverLib.Compressor;
using ArrArchiverLib.Decompressor;
using ArrArchiverLib.Metadata.Models;

namespace ArrArchiverLib.Archiver
{
    public interface IArchiver
    {
        public ICompressor Compressor { get; }
        public IDecompressor Decompressor { get; }
        public Task CreateAsync(string inputPath, string outputPath);
        public Task ExtractAsync(string inputPath, string outputPath);
        public Task<ArchiveInfo> GetArchiveInfoAsync(string path);
        public Task<List<DirectoryHeader>> GetDirectoriesAsync(string path);
        public Task<List<FileHeader>> GetFilesAsync(string path);
        public Task<bool> IsEncryptedAsync(string path);
        public Task<bool> IsArchiveAsync(string path);
    }
}