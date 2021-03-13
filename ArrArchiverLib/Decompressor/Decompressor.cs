using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ArrArchiverLib.Compressor;
using ArrArchiverLib.Exceptions;
using ArrArchiverLib.Extensions;
using ArrArchiverLib.Metadata.Models;
using ArrArchiverLib.Progress;
using ArrArchiverLib.Resources;
using ArrArchiverLib.Streams;
using NeoSmart.AsyncLock;

namespace ArrArchiverLib.Decompressor
{
    public class Decompressor : IDecompressor
    {
        private readonly IArchiveProgress _archiveProgress;
        private readonly AsyncLock _asyncLock;
       
        private ArchiveStreamBase _inputStream;
        
        public DecompressorSettings Settings { get; }
        
        public Decompressor(IArchiveProgress archiveProgress, DecompressorSettings settings = null)
        {
            _archiveProgress = archiveProgress;
            _asyncLock = new AsyncLock();
            Settings = settings ?? new DecompressorSettings();
        }

        public async Task DecompressAsync(ArchiveStreamBase inputStream, List<FileHeader> fileHeaders)
        {
            _inputStream = inputStream;
            _archiveProgress.Start(fileHeaders);
            
            var groupedFileHeaders = fileHeaders.Batch(Environment.ProcessorCount);
            
            foreach (var headers in groupedFileHeaders)
            {
                var tasks = headers.Select(DecompressFileAsync);
                
                await Task.WhenAll(tasks);
            }

            _inputStream = null;
            _archiveProgress.Reset();
        }

        private async Task DecompressFileAsync(FileHeader fileHeader)
        {
            await using var outputStream = ArchiveStream.Create(fileHeader.FullPath);
            var chunkDecompressor = GetChunkDecompressor(fileHeader.IsEncrypted);

            foreach (var chunk in fileHeader.Chunks)
            {
                byte[] compressChunk;

                using (await _asyncLock.LockAsync())
                {
                    _inputStream.Position = fileHeader.Position;
                    compressChunk = await _inputStream.ReadBytesAsync(chunk.Size);
                }
                
                fileHeader.Position += chunk.Size;
                
                try
                {
                    await chunkDecompressor(compressChunk, outputStream, fileHeader.CompressionType);
                }
                catch
                {
                    if (fileHeader.IsEncrypted)
                    {
                        throw new ArchiveException(ExceptionResource.InvalidEncryptKey);
                    }
                    throw new ArchiveException(ExceptionResource.ThisFileIsCorrupted);
                }
                
                _archiveProgress.Report(fileHeader.RelativePath, chunk.Size, chunk.SerialNumber, fileHeader.NumberOfChunks);
            }
        }

        private Func<byte[], Stream, CompressionType, Task> GetChunkDecompressor(bool isEncrypted)
        {
            return isEncrypted 
                ? DecompressAndDecryptChunkAsync
                : DecompressChunkAsync;
        }
        
        private async Task DecompressChunkAsync(byte[] chunk, Stream outputStream, CompressionType compressionType)
        {
            await using var memoryStream = new MemoryStream(chunk);
            await using var decompressedStream = GetDecompressionStream(memoryStream, compressionType);
            
            await decompressedStream.CopyToAsync(outputStream);
        }

        private async Task DecompressAndDecryptChunkAsync(byte[] chunk, Stream outputStream, CompressionType compressionType)
        {
            if (Settings.EncryptKey == null)
            {
                throw new ArchiveException(ExceptionResource.EmptyEncryptKey);
            }
            
            var salt = chunk.Skip(chunk.Length - 16).ToArray();
            chunk = chunk.Take(chunk.Length - 16).ToArray();
            
            var aesManaged = new  AesManaged().Initialize(Settings.EncryptKey, salt);
            await using var memoryStream = new MemoryStream(chunk);
            await using var decryptStream =  new CryptoStream(memoryStream, aesManaged.CreateDecryptor(), CryptoStreamMode.Read);
            await using var decompressedStream = GetDecompressionStream(decryptStream, compressionType);
            
            await decompressedStream.CopyToAsync(outputStream);
        }
        
        private Stream GetDecompressionStream(Stream stream, CompressionType compressionType)
        {
            var streamForCompression = (compressionType == CompressionType.Deflate)
                ? new GZipStream(stream, CompressionMode.Decompress) 
                : new BrotliStream(stream, CompressionMode.Decompress) as Stream;

            return streamForCompression;
        }
    }
}