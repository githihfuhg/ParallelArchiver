using System.Collections.Generic;
using System.Threading.Tasks;
using ArrArchiverLib.Metadata.Models;
using ArrArchiverLib.Streams;

namespace ArrArchiverLib.Compressor
{
    public interface ICompressor
    {
        CompressorSettings Settings { get; }
        Task CompressAsync(List<FileHeader> fileHeaders, ArchiveStreamBase outputStream);
    }
    
}