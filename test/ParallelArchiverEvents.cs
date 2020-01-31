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
        protected long NumberOfFiles = 0;
        private long AllFilesLength = 0;
        private long Counter = 0;
        private int ProgressFile = 0;
        private int FullProgress = 0;

        protected void AddProgressFile(string name,long blockLength, long streamReadLength = 0, long streamReadPosition = 0)
        {
            Counter += blockLength;
            ProgressFile = (streamReadLength > 0 && streamReadPosition > 0) ? (int)(streamReadPosition * 100 / streamReadLength) : 100; 
            FullProgress = (int)(Counter * 100 / AllFilesLength);
            Progress?.Invoke(name, ProgressFile, FullProgress);
        }
        //protected void AddProgressFile(string name, long blockLength)
        //{
        //    Counter += blockLength;
        //    ProgressFile = (streamReadLength > 0 && streamReadPosition > 0) ? (int)(streamReadPosition * 100 / streamReadLength) : 100;
        //    FullProgress = (int)(Counter * 100 / AllFilesLength);
        //    //Progress?.Invoke(name, ProgressFile, FullProgress);
        //}

        protected void Start(IEnumerable<FileInfo> fileInfo)
        {
            AllFilesLength = fileInfo.Select(path => path.Length).Sum();
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
        // еще тут будет логика вычисления значений, передаваемых оработчику
      }
}
