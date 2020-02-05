using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace test
{

    //public class ProgressCounter
    //{
    //    private long NumberOfFiles { get; set; } = 0;
    //    private long AllFilesLength { get; set; } = 0;
    //    private long Counter { get; set; } = 0;
    //    public int ProgressFile { get; set; } = 0;
    //    public int FullProgress { get; set; } = 0;

    //    private bool Seted = false;

    //    public ProgressCounter(IEnumerable<TFile> fileInfo)
    //    {
    //        Start(fileInfo);
    //    }

    //    public ProgressCounter(IEnumerable<FileInfo> fileInfo)
    //    {
    //        Start(fileInfo);
    //    }

    //    public  void AddProgressFile(string name, long blockLength, long streamReadLength = 0,
    //        long streamReadPosition = 0)
    //    {
    //        Seted = true;
    //        Counter += blockLength;
    //        ProgressFile = (streamReadLength > 0 && streamReadPosition > 0)
    //            ? (int) (streamReadPosition * 100 / streamReadLength)
    //            : 100;
    //        FullProgress = (int) (Counter * 100 / AllFilesLength);
    //    }
    //    private void Start(IEnumerable<FileInfo> fileInfo)
    //    {
    //        AllFilesLength = fileInfo.Select(path =>
    //            path.Length).Sum();
    //        Counter = 0;
    //    }
    //    private void Start(IEnumerable<TFile> tFile)
    //    {
    //        AllFilesLength = tFile.Select(tF => tF.FileLength).Sum();
    //        Counter = 0;
    //    }
    //}

    public class ParallelArchiverEvents
    {
        //public event Action<string, int, int> Progress;
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

    public class ProgressEventArgs
    {
        public int FullProgress { get; set; }
        public string FileName { get; set; }
        public int CurrentFileProcent { get; set; }
    }
}
