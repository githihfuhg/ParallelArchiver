using System;
using System.Collections.Generic;
using System.Text;

namespace test
{
    public class ParallelArchiverEvents
    {
        public event Action<string, int, int> Progress;

        protected void AddProgressFile(string name, int progressFile, int fullProgress)
        {
            Progress?.Invoke(name, progressFile, fullProgress);
        }
    }
}
