using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArrArchiverLib.Metadata.Models;
using ArrArchiverLib.Streams;

namespace ArrArchiverLib.Decompressor
{
    public interface IDecompressor 
    {
        DecompressorSettings Settings { get; }
        Task DecompressAsync(ArchiveStreamBase inputStream, List<FileHeader> fileHeaders);
    }
}