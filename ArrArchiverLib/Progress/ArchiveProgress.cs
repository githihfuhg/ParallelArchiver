using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ArrArchiverLib.Metadata.Models;

namespace ArrArchiverLib.Progress
{
    public class ArchiveProgress : Progress<ArchiveProgressEventArgs>, IArchiveProgress
    {
        private readonly Stopwatch _stopwatch;
        
        private long _sizeOfAllFiles = 0;
        private long _counter = 0;
        
        internal ArchiveProgress()
        {
            _stopwatch = new Stopwatch();
        }

        public void Report(string fileName, int chunkSize, int chunkSerialNumber, int numberOfAllChunks)
        {
            _counter += chunkSize;
            
            var currentFileProgress = (numberOfAllChunks > 1) ? chunkSerialNumber * 100 / numberOfAllChunks : 100;
            var allProgress = (int) (_counter * 100 / _sizeOfAllFiles);
            
            var progress = new ArchiveProgressEventArgs()
            {
                CurrentFileProgress = currentFileProgress,
                AllProgress = allProgress,
                FileName = fileName,
                ElapsedMilliseconds = _stopwatch.ElapsedMilliseconds
            };
            
            base.OnReport(progress);
        }

        public void Start(IEnumerable<FileHeader> fileHeaders)
        {
            _sizeOfAllFiles = fileHeaders.Sum(x => x.FileSize);
            _counter = 0;
            _stopwatch.Start();
        }

        public void Reset()
        {
            _stopwatch.Reset();
            _counter = 0;
            _sizeOfAllFiles = 0;
        }
    }
}