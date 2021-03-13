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
    public abstract class ArchiveStreamBase : Stream
    {
        private Stream _stream;
        public int ThreadsCount { get; }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        protected ArchiveStreamBase(string path, FileMode mode, FileAccess access, FileShare share, int threadsCount)
        {
            _stream = new FileStream(path, mode, access, share);
            ThreadsCount = threadsCount;
        }

        public override void Flush() => _stream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override void SetLength(long value) => _stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            _stream?.Dispose();
            _stream = null;
        }

        public int GetChunksCount(int chunkLength = 4718592) => GenerateDegreeOfParallelism(chunkLength) * ThreadsCount;

        public int ReadInt() => BitConverter.ToInt32(ReadBytes(sizeof(int)));
        
        public bool ReadBoolean() => BitConverter.ToBoolean(ReadBytes(sizeof(bool)));
        
        public long ReadLong() => BitConverter.ToInt64(ReadBytes(sizeof(long)));

        public string ReadString(int lenght, Encoding encoding) => encoding.GetString(ReadBytes(lenght));

        public virtual ArchiveInfo ReadArchiveInfo()
        {
            _stream.Position = 0;

            try
            {
                var archiveInfo = new ArchiveInfo();
                archiveInfo.Header = ReadString(archiveInfo.HeaderLenght, Encoding.UTF8);
                archiveInfo.IsEncrypted = ReadBoolean();
                archiveInfo.NumberOfDirectories = ReadInt();
                archiveInfo.DirectoriesBlockPosition = ReadInt();
                archiveInfo.NumberOfFiles = ReadInt();
                archiveInfo.FilesBlockPosition = ReadInt();

                if (archiveInfo.Header != ArchiveResource.ArchiveHeader)
                {
                    throw new ArchiveException(ExceptionResource.FileIsNotArchive);
                }

                return archiveInfo;
            }
            catch
            {
                throw new ArchiveException(ExceptionResource.FileIsNotArchive);
            }
        }

        public Task<ArchiveInfo> ReadArchiveInfoAsync() => Task.Run(ReadArchiveInfo);

        public virtual DirectoryHeader ReadDirectory(string outputPath, long streamPosition = 0)
        {
            if (streamPosition != 0)
            {
                Position = streamPosition;
            }

            var pathLength = ReadInt();

            var directoryHeader = new DirectoryHeader()
            {
                RelativePathLength = pathLength,
                RelativePath = ReadString(pathLength, Encoding.UTF8)
            };

            directoryHeader.FullPath = Path.Combine(outputPath, directoryHeader.RelativePath);

            return directoryHeader;
        }

        public Task<DirectoryHeader> ReadDirectoryAsync(string outputPath, long streamPosition = 0) => Task.Run(() => ReadDirectory(outputPath, streamPosition));

        public virtual IEnumerable<DirectoryHeader> ReadAllDirectories(string outputPath)
        {
            var archiveInfo = ReadArchiveInfo();
            _stream.Position = archiveInfo.DirectoriesBlockPosition;

            for (var i = 0; i < archiveInfo.NumberOfDirectories; i++)
            {
                yield return ReadDirectory(outputPath);
            }
        }

        public Task<IEnumerable<DirectoryHeader>> ReadAllDirectoriesAsync(string outputPath) => Task.Run(() => ReadAllDirectories(outputPath));

        public virtual FileHeader ReadFileHeader(string outputPath)
        {
            var fileHeader = new FileHeader();
            fileHeader.IsEncrypted = ReadBoolean();
            fileHeader.CompressionType = (CompressionType) ReadInt();
            fileHeader.RelativePathLength = ReadInt();
            fileHeader.RelativePath = ReadString(fileHeader.RelativePathLength, Encoding.UTF8);
            fileHeader.FullPath = Path.Combine(outputPath, fileHeader.RelativePath);
            fileHeader.Position = ReadLong();
            fileHeader.NumberOfChunks = ReadInt();
            fileHeader.Chunks = new List<ChunkHeader>(fileHeader.NumberOfChunks);
            fileHeader.FileSize = 0;

            for (var j = 0; j < fileHeader.NumberOfChunks; j++)
            {
                var chunk = ReadChunkHeader();
                fileHeader.Chunks.Add(chunk);
                fileHeader.FileSize += chunk.Size;
            }

            return fileHeader;
        }

        public Task<FileHeader> ReadFileHeaderAsync(string outputPath) => Task.Run(() => ReadFileHeader(outputPath));

        public virtual IEnumerable<FileHeader> ReadAllFileHeaders(string outputPath)
        {
            var archiveInfo = ReadArchiveInfo();
            _stream.Position = archiveInfo.FilesBlockPosition;

            for (var i = 0; i < archiveInfo.NumberOfFiles; i++)
            {
                var fileHeader = ReadFileHeader(outputPath);

                Seek(fileHeader.FileSize, SeekOrigin.Current);

                yield return fileHeader;
            }
        }

        public Task<IEnumerable<FileHeader>> ReadAllFileHeadersAsync(string outputPath) => Task.Run(() => ReadAllFileHeaders(outputPath));

        public virtual ChunkHeader ReadChunkHeader()
        {
            return new ChunkHeader()
            {
                SerialNumber = ReadInt(),
                Size = ReadInt()
            };
        }

        public Task<ChunkHeader> ReadChunkHeaderAsync() => Task.Run(ReadChunkHeader);

        public virtual async IAsyncEnumerable<byte[][]> ReadFileInChunksAsync(int chunkLength = 4718592)
        {
            var degreeOfParallelism = GenerateDegreeOfParallelism(chunkLength);
            var sizeBigChunks = GenerateAndBalancingChunksSize(Length, degreeOfParallelism);

            foreach (var sizeBigChunk in sizeBigChunks)
            {
                var sizeChunks = GenerateAndBalancingChunksSize(sizeBigChunk, ThreadsCount);

                var chunks = new byte[sizeChunks.Length][];

                for (var i = 0; i < sizeChunks.Length; i++)
                {
                    chunks[i] = await ReadBytesAsync(sizeChunks[i]);
                }

                yield return chunks;
            }
        }

        public void WriteInt(int value) => _stream.Write(BitConverter.GetBytes(value));

        public void WriteLong(long value) => _stream.Write(BitConverter.GetBytes(value));

        public void WriteString(string value) => _stream.Write(Encoding.UTF8.GetBytes(value));
        
        public void WriteBoolean(bool value) => _stream.Write(BitConverter.GetBytes(value));

        public virtual void WriteArchiveInfo(ArchiveInfo archiveInfo)
        {
            _stream.Position = 0;
            WriteString(archiveInfo.Header);
            WriteBoolean(archiveInfo.IsEncrypted);
            WriteInt(archiveInfo.NumberOfDirectories);
            WriteInt(archiveInfo.DirectoriesBlockPosition);
            WriteInt(archiveInfo.NumberOfFiles);
            WriteLong(archiveInfo.FilesBlockPosition);
        }

        public Task WriteArchiveInfoAsync(ArchiveInfo archiveInfo) => Task.Run(() => WriteArchiveInfo(archiveInfo));

        public virtual void WriteDirectory(DirectoryHeader directoryHeaderInfo, long streamPosition = 0)
        {
            if (streamPosition != 0)
            {
                Position = streamPosition;
            }

            WriteInt(directoryHeaderInfo.RelativePathLength);
            WriteString(directoryHeaderInfo.RelativePath);
        }

        public Task WriteDirectoryAsync(DirectoryHeader directoryHeaderInfo, long streamPosition = 0) => Task.Run(() => WriteDirectory(directoryHeaderInfo, streamPosition));

        public virtual void WriteDirectories(IEnumerable<DirectoryHeader> directories)
        {
            foreach (var directory in directories)
            {
                WriteDirectory(directory);
            }
        }

        public Task WriteDirectoriesAsync(IEnumerable<DirectoryHeader> directories) => Task.Run(() => WriteDirectories(directories));

        public virtual void WriteFileHeader(FileHeader fileHeader)
        {
            WriteBoolean(fileHeader.IsEncrypted);
            WriteInt((int) fileHeader.CompressionType);
            WriteInt(fileHeader.RelativePathLength);
            WriteString(fileHeader.RelativePath);
            WriteLong(fileHeader.Position);
            WriteInt(fileHeader.NumberOfChunks);
            fileHeader.Chunks.ForEach(WriteChunkHeader);
        }

        public Task WriteFileHeaderAsync(FileHeader fileHeader) => Task.Run(() => WriteFileHeader(fileHeader));

        public virtual void WriteChunkHeader(ChunkHeader chunkHeader)
        {
            WriteInt(chunkHeader.SerialNumber);
            WriteInt(chunkHeader.Size);
        }

        public Task WriteChunkHeaderAsync(ChunkHeader chunkHeader) => Task.Run(() => WriteChunkHeader(chunkHeader));

        public virtual byte[] ReadBytes(long count)
        {
            if (_stream.Position < 0)
            {
                throw new EndOfStreamException();
            }

            var buffer = new byte[count];
            _stream.Read(buffer);

            return buffer;
        }

        public Task<byte[]> ReadBytesAsync(long numberOfByte) => Task.Run(() => ReadBytes(numberOfByte));

        private int GenerateDegreeOfParallelism(int chunkLength = 4718592)
        {
            var result = (float) (Length / ThreadsCount) / chunkLength;

            if (result % 10 == 0 || result < 1)
            {
                result++;
            }

            return (int) result;
        }

        private long[] GenerateAndBalancingChunksSize(long fileLength, int numberOfChunks)
        {
            var blocks = Enumerable.Range(0, numberOfChunks)
                .Select(x => fileLength / numberOfChunks).ToArray();

            var blocksSum = blocks.Sum();

            if (blocksSum != fileLength)
            {
                blocks[^1] += fileLength - blocksSum;
            }

            return blocks;
        }
    }
}