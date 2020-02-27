using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace test
{

    public class ParallelArchiverEvents
    {
        public event EventHandler<ProgressEventArgs> Progress;
        private long AllFilesLength { get; set; } = 0;
        private long Counter { get; set; } = 0;

        internal void AddProgressFile(string name, long blockLength, long streamReadLength = 0, long streamReadPosition = 0)
        {
            Counter += blockLength;
            Progress?.Invoke(this, new ProgressEventArgs()
            {
                FileName = name,
                CurrentFileProcent = (streamReadLength > 0 && streamReadPosition > 0) ? (int)(streamReadPosition * 100 / streamReadLength) : 100,
                FullProgress = (int)(Counter * 100 / AllFilesLength)
            });
        }

        internal void Start(IEnumerable<FileInfo> fileInfo)
        {
            AllFilesLength = fileInfo.Select(path =>
                path.Length).Sum();
            Counter = 0;
        }

        internal void Start(IEnumerable<TFile> tFile)
        {
            AllFilesLength = tFile.Select(tF => tF.FileLength).Sum();
            Counter = 0;
        }

        internal void Restart()
        {
            AllFilesLength = 0;
            Counter = 0;
        }
    }

    public class ProgressEventArgs : EventArgs
    {
        public int FullProgress { get; set; }
        public string FileName { get; set; }
        public int CurrentFileProcent { get; set; }
    }
}
