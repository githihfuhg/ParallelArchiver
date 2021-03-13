using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ArrArchiverLib.Exceptions;
using ArrArchiverLib.Extensions;
using ArrArchiverLib.Metadata.Models;
using ArrArchiverLib.Progress;
using ArrArchiverLib.Resources;
using ArrArchiverLib.Streams;
using NeoSmart.AsyncLock;

namespace ArrArchiverLib.Compressor
{
    public class Compressor : ICompressor
    {
        private readonly AsyncLock _asyncLock;
        private readonly IArchiveProgress _archiveProgress;

        private ArchiveStreamBase _outputStream;
        public CompressorSettings Settings { get; }

        public Compressor(IArchiveProgress archiveProgress, CompressorSettings settings = null)
        {
            Settings = settings ?? new CompressorSettings()
            {
                ThreadsCount = Environment.ProcessorCount,
                ChunkSize = 4718592,
                CompressionLevel = CompressionLevel.Optimal,
                TextFileExtensions = ArchiveResource.TextExtensions.Split(",")
            };
            _archiveProgress = archiveProgress;
            _asyncLock = new AsyncLock();
        }

        public async Task CompressAsync(List<FileHeader> fileHeaders, ArchiveStreamBase outputStream)
        {
            _outputStream = outputStream;
            _archiveProgress.Start(fileHeaders);

            var smallFiles = new List<FileHeader>();

            foreach (var fileHeader in fileHeaders)
            {
                fileHeader.IsEncrypted = Settings.IsEncryptEnable;
                fileHeader.CompressionType = GenerateCompressionType(fileHeader.RelativePath);

                if (fileHeader.FileSize > Settings.ChunkSize)
                {
                    await CompressBigFileAsync(fileHeader);
                }
                else
                {
                    smallFiles.Add(fileHeader);
                }
            }

            await CompressSmallFiles(smallFiles);

            _outputStream = null;
            _archiveProgress.Reset();
        }

        private async Task CompressSmallFiles(List<FileHeader> fileHeaders)
        {
            var groupedFileHeaders = fileHeaders.Batch(Settings.ThreadsCount);

            foreach (var headers in groupedFileHeaders)
            {
                var tasks = headers.Select(CompressSmallFile);

                await Task.WhenAll(tasks);
            }
        }

        private async Task CompressSmallFile(FileHeader header)
        {
            header.NumberOfChunks = 1;
            await using var readStream = ArchiveStream.OpenRead(header.FullPath);
            var chunkCompressor = GetChunkCompressor();
            var chunk = await readStream.ReadBytesAsync(header.FileSize);
            var compressedChunk = await chunkCompressor(chunk, header.CompressionType);

            var chunkHeader = new ChunkHeader()
            {
                Size = compressedChunk.Length,
                SerialNumber = 0
            };

            header.Chunks.Add(chunkHeader);

            using (await _asyncLock.LockAsync())
            {
                header.Position = _outputStream.Position + header.SizeOf;
                await _outputStream.WriteFileHeaderAsync(header);
                await _outputStream.WriteAsync(compressedChunk);
            }

            _archiveProgress.Report(header.RelativePath, chunk.Length, chunkHeader.SerialNumber, header.NumberOfChunks);
        }

        private async Task CompressBigFileAsync(FileHeader header)
        {
            await using var readStream = ArchiveStream.OpenRead(header.FullPath, Settings.ThreadsCount);
            var chunkCompressor = GetChunkCompressor();
            header.NumberOfChunks = readStream.GetChunksCount();
            _outputStream.Seek(header.SizeOf, SeekOrigin.Current);

            header.Position = _outputStream.Position;
            var chunkSerialNumber = 0;

            await foreach (var chunks in readStream.ReadFileInChunksAsync(Settings.ChunkSize))
            {
                var tasks = chunks.Select(x => chunkCompressor(x, header.CompressionType));
                var chunksSizes = chunks.Select(x => x.Length).ToArray();
                var compressedChunks = await Task.WhenAll(tasks);

                for (var i = 0; i < compressedChunks.Length; i++)
                {
                    var chunkHeader = new ChunkHeader()
                    {
                        Size = compressedChunks[i].Length,
                        SerialNumber = chunkSerialNumber++,
                    };

                    header.Chunks.Add(chunkHeader);
                    await _outputStream.WriteAsync(compressedChunks[i]);

                    _archiveProgress.Report(header.RelativePath, chunksSizes[i], chunkHeader.SerialNumber,
                        header.NumberOfChunks);
                }

                var currentPosition = _outputStream.Position;
                _outputStream.Position = header.Position - header.SizeOf;

                await _outputStream.WriteFileHeaderAsync(header);

                _outputStream.Position = currentPosition;
            }
        }


        private Func<byte[], CompressionType, Task<byte[]>> GetChunkCompressor()
        {
            return Settings.IsEncryptEnable
                ? CompressAndEncryptChunk
                : CompressChunkAsync;
        }

        private Task<byte[]> CompressChunkAsync(byte[] chunk, CompressionType compressionType)
        {
            return Task.Run(() =>
            {
                using var memoryStream = new MemoryStream();
                using var compressionStream = GetCompressionStream(memoryStream, compressionType);

                compressionStream.Write(chunk);
                compressionStream.Close();
                return memoryStream.ToArray();
            });
        }
        
        private Task<byte[]> CompressAndEncryptChunk(byte[] chunk, CompressionType compressionType)
        {
            return Task.Run(() =>
            {
                using var memoryStream = new MemoryStream();
                
                var aesManaged = new AesManaged().Initialize(Settings.EncryptKey);
                using var encryptStream = new CryptoStream(memoryStream, aesManaged.CreateEncryptor(), CryptoStreamMode.Write);
                using var compressionStream = GetCompressionStream(encryptStream, compressionType);

                compressionStream.Write(chunk);
                compressionStream.Close();

                var compressAndEncryptChunk = memoryStream.ToArray();
                return compressAndEncryptChunk.Concat(aesManaged.IV).ToArray();;
            });
        }

        private Stream GetCompressionStream(Stream stream, CompressionType compressionType)
        {
            var compressionLevel = (compressionType == CompressionType.Deflate)
                ? Settings.CompressionLevel
                : CompressionLevel.Fastest;

            var streamForCompression = (compressionType == CompressionType.Deflate)
                ? new GZipStream(stream, compressionLevel) as Stream
                : new BrotliStream(stream, compressionLevel);

            return streamForCompression;
        }

        private CompressionType GenerateCompressionType(string path)
        {
            var isTextFile = Settings.TextFileExtensions.Contains(Path.GetExtension(path));

            return isTextFile
                ? CompressionType.Brotli
                : CompressionType.Deflate;
        }
    }
}