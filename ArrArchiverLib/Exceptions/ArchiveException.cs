using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrArchiverLib.Exceptions
{
    public class ArchiveException : Exception
    {
        public ArchiveException(string message) : base(message)
        {
        
        }
    }
}