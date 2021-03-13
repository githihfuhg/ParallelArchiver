using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArrArchiverLib.Compressor;
using ArrArchiverLib.Exceptions;
using ArrArchiverLib.Metadata.Models;
using ArrArchiverLib.Resources;

namespace ArrArchiverLib.Streams
{
    public class ArchiveStream : ArchiveStreamBase
    {
        public ArchiveStream(string path, FileMode mode, FileAccess access, FileShare share, int threadsCount ) : base(path, mode, access, share, threadsCount)
        {
        }
        
        public ArchiveStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access, share, Environment.ProcessorCount)
        {
        }
        
        public static ArchiveStream Create(string path) =>
            new ArchiveStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        public static ArchiveStream OpenRead(string path, int threadsCount) =>
            new(path, FileMode.Open, FileAccess.Read, FileShare.Read, threadsCount);
        
        public static ArchiveStream OpenRead(string path) =>
            new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        public static ArchiveStream OpenWrite(string path) =>
            new(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
    }
}