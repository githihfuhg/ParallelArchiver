using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArrArchiverLib.Metadata.Models;


namespace ArrArchiverLib.Metadata
{
    public class Metadata : IMetadata
    {
        private readonly DirectoryInfo _mainDirectoryInfo;
        private readonly char _directorySeparator;
        private readonly string _path;

        public Metadata(string path)
        {
            _path = path;
            
            var isDirectory = File.GetAttributes(path).HasFlag(FileAttributes.Directory);

            if (isDirectory)
            {
                _mainDirectoryInfo = new DirectoryInfo(path);
            }

            _directorySeparator = Path.DirectorySeparatorChar;
        }

        public List<DirectoryHeader> GenerateDirectoryHeaders()
        {
            if (_mainDirectoryInfo == null)
            {
                return new List<DirectoryHeader>();
            }
            
            var directoryHeaders = _mainDirectoryInfo
                .EnumerateDirectories("*", SearchOption.AllDirectories)
                .AsParallel()
                .Select(GenerateDirectoryHeader).ToList();
            
            directoryHeaders.Add(GenerateDirectoryHeader(_mainDirectoryInfo));

            return directoryHeaders;
        }
        
        public Task<List<DirectoryHeader>> GenerateDirectoryHeadersAsync() => Task.Run(GenerateDirectoryHeaders);

        public List<FileHeader> GenerateFileHeaders()
        {
            var files = _mainDirectoryInfo?
                .EnumerateFiles("*", SearchOption.AllDirectories)
                        ?? new[] {new FileInfo(_path)};
            
            return files.AsParallel().Select(GenerateFileHeader).ToList();
        }
        
        public Task<List<FileHeader>> GenerateFileHeadersAsync() => Task.Run(GenerateFileHeaders);

        private DirectoryHeader GenerateDirectoryHeader(DirectoryInfo directoryInfo)
        {
            var relativePath = (directoryInfo == _mainDirectoryInfo) 
                ? Path.Combine(_mainDirectoryInfo.Name, directoryInfo.FullName.Replace($"{_mainDirectoryInfo.FullName}", "")) + _directorySeparator 
                : Path.Combine(_mainDirectoryInfo.Name, directoryInfo.FullName.Replace($"{_mainDirectoryInfo.FullName}{_directorySeparator}", "")) + _directorySeparator;

            return new DirectoryHeader
            {
                RelativePathLength = Encoding.UTF8.GetBytes(relativePath).Length,
                RelativePath = relativePath,
                FullPath = directoryInfo.FullName,
            };
        }

        private FileHeader GenerateFileHeader(FileInfo fileInfo)
        {
            var relativePath = _mainDirectoryInfo != null
                ? Path.Combine(_mainDirectoryInfo.Name, fileInfo.FullName.Replace($"{_mainDirectoryInfo.FullName}{_directorySeparator}", ""))
                : fileInfo.Name;

            return new  FileHeader()
            {
                RelativePathLength = Encoding.UTF8.GetBytes(relativePath).Length,
                RelativePath = relativePath,
                FullPath = fileInfo.FullName,
                FileSize = fileInfo.Length,
                Chunks = new List<ChunkHeader>()
            };
        }
    }
}