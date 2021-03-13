using System.Collections.Generic;
using System.Threading.Tasks;
using ArrArchiverLib.Metadata.Models;

namespace ArrArchiverLib.Metadata
{
    public interface IMetadata
    {
        public List<DirectoryHeader> GenerateDirectoryHeaders();
        public List<FileHeader> GenerateFileHeaders();
        public Task<List<DirectoryHeader>> GenerateDirectoryHeadersAsync();
        public Task<List<FileHeader>> GenerateFileHeadersAsync();
    }
}