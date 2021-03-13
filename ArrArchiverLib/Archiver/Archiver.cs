using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArrArchiverLib.Compressor;
using ArrArchiverLib.Decompressor;
using ArrArchiverLib.Exceptions;
using ArrArchiverLib.Metadata.Models;
using ArrArchiverLib.Progress;
using ArrArchiverLib.Resources;
using ArrArchiverLib.Streams;

namespace ArrArchiverLib.Archiver
{
    public class Archiver : IArchiver
    {
        public ICompressor Compressor { get; }
        public IDecompressor Decompressor { get; }
        public Progress<ArchiveProgressEventArgs> Progress { get; }

        public Archiver()
        {
            var progress = new ArchiveProgress();
            
            Progress = progress;
            Compressor = new Compressor.Compressor(progress);
            Decompressor = new Decompressor.Decompressor(progress);
        }

        public async Task CreateAsync(string inputPath, string outputPath)
        {
            await using var outputStream = ArchiveStream.Create(outputPath);

            var metadata = new Metadata.Metadata(inputPath);
            var directoryHeaders = await metadata.GenerateDirectoryHeadersAsync();
            var fileHeaders = await metadata.GenerateFileHeadersAsync();
            
            var archiveInfo = new ArchiveInfo()
            {
                Header = ArchiveResource.ArchiveHeader,
                IsEncrypted = Compressor.Settings.IsEncryptEnable,
                NumberOfDirectories = directoryHeaders.Count,
                NumberOfFiles = fileHeaders.Count,
            };

            archiveInfo.DirectoriesBlockPosition = archiveInfo.SizeOf;
            outputStream.Seek(archiveInfo.SizeOf, SeekOrigin.Current);

            await outputStream.WriteDirectoriesAsync(directoryHeaders);
            archiveInfo.FilesBlockPosition = outputStream.Position;
            
            await outputStream.WriteArchiveInfoAsync(archiveInfo);
            outputStream.Position = archiveInfo.FilesBlockPosition;

            await Compressor.CompressAsync(fileHeaders, outputStream);
        }

        public async Task ExtractAsync(string inputPath, string outputPath)
        {
            await using var stream = ArchiveStream.OpenRead(inputPath);

            var directoryHeaders = (await stream.ReadAllDirectoriesAsync(outputPath)).ToList();
            var fileHeaders = (await stream.ReadAllFileHeadersAsync(outputPath)).ToList();
            
            directoryHeaders.AsParallel().ForAll(x => 
            {
                if (!string.IsNullOrEmpty(x.FullPath) && !Directory.Exists(x.FullPath))
                {
                    Directory.CreateDirectory(x.FullPath);
                }
            });

            try
            {
                await Decompressor.DecompressAsync(stream, fileHeaders);
            }
            catch(Exception ex)
            {
                await DeleteFiles(fileHeaders);
                await DeleteDirectories(directoryHeaders);
                
                throw new ArchiveException(ex.Message);
            }
        }

        public async Task<List<FileHeader>> GetFilesAsync(string path)
        {
            await using var stream = ArchiveStream.OpenRead(path);

            var fileHeaders = await stream.ReadAllFileHeadersAsync(path);

            return fileHeaders.ToList();
        }

        public async Task<List<DirectoryHeader>> GetDirectoriesAsync(string path)
        {
            await using var stream = ArchiveStream.OpenRead(path);

            var directoryHeaders = await stream.ReadAllDirectoriesAsync(path);

            return directoryHeaders.ToList();
        }

        public async Task<ArchiveInfo> GetArchiveInfoAsync(string path)
        {
            await using var stream = ArchiveStream.OpenRead(path);
            
            var archiveInfo = await stream.ReadArchiveInfoAsync();

            return archiveInfo;
        }

        public async Task<bool> IsEncryptedAsync(string path)
        {
            await using var stream = ArchiveStream.OpenRead(path);
            
            var archiveInfo = await stream.ReadArchiveInfoAsync();
            
            return archiveInfo.IsEncrypted;
        }

        public async Task<bool> IsArchiveAsync(string path)
        {
            await using var stream = ArchiveStream.OpenRead(path);
            
            try
            {
                await stream.ReadArchiveInfoAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private Task DeleteDirectories(List<DirectoryHeader> directoryHeaders)
        {
            return Task.Run(() =>
            {
                directoryHeaders.ForEach(x =>
                {
                    if (Directory.Exists(x.FullPath) && Directory.GetFiles(x.FullPath).Length == 0)
                    {
                        Directory.Delete(x.FullPath, true);
                    }
                });
            });
        }
        
        private Task DeleteFiles(List<FileHeader> fileHeaders)
        {
           return Task.Run(() => fileHeaders.AsParallel().ForAll(x => File.Delete(x.FullPath)));
        }
        
    }
}