using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test
{
      internal class ParallelArchiverEvents
      {
        public event Action<string, int, int> Progress;
        protected long NumberOfFiles { get; set; } = 0;
        private long AllFilesLength { get; set; } = 0;
        private long Counter { get; set; } = 0;
        private int ProgressFile { get; set; } = 0;
        private int FullProgress { get; set; } = 0;

        protected void AddProgressFile(string name,long blockLength, long streamReadLength = 0, long streamReadPosition = 0)
        {
            Counter += blockLength;
            ProgressFile = (streamReadLength > 0 && streamReadPosition > 0) ? (int)(streamReadPosition * 100 / streamReadLength) : 100; 
            FullProgress = (int)(Counter * 100 / AllFilesLength);
            Progress?.Invoke(name, ProgressFile, FullProgress);
        }
        
        protected void Start(IEnumerable<FileInfo> fileInfo)
        {
            AllFilesLength = fileInfo.Select(path => 
                path.Length).Sum();
            Counter = 0;
        }
        protected void Start(IEnumerable<TFile> tFile)
        {
            AllFilesLength = tFile.Select(tF => tF.FileLength).Sum();
            Counter = 0;
        }
        protected void Restart()
        {
            AllFilesLength = 0;
            Counter = 0;
        }
      }
}
