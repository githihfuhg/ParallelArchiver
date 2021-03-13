using System;
using System.Collections.Generic;
using ArrArchiverLib.Metadata.Models;

namespace ArrArchiverLib.Progress
{
    public interface IArchiveProgress : IProgress<ArchiveProgressEventArgs>
    {
        public void Report(string fileName, int chunkSize, int chunkSerialNumber, int numberOfAllChunks);
        public void Start(IEnumerable<FileHeader> fileHeaders);
        public void Reset();
    }
}