using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace test
{
    internal class Title
    {

        private FileStream Stream { get; }

        public Title(FileStream stream)
        {
            Stream = stream;
        }

        public void AddTitleDirectories(DirectoryInfo mainDir,bool oneFile = false)
        {
            var directories = (oneFile) ? new List<DirectoryInfo>() : mainDir.EnumerateDirectories("*", SearchOption.AllDirectories).ToList();
            directories.Add(mainDir);
            var type = (oneFile) ? "fil" : "dir";
            Stream.Write(Encoding.UTF8.GetBytes(type), 0, 3);
            Stream.Seek(8, SeekOrigin.Current);
            Stream.Write(BitConverter.GetBytes(directories.Count()), 0, 4);
            foreach (var dir in directories)
            {
                var FileName = (dir == mainDir) ?
                    Path.Combine(mainDir.Name, dir.FullName.Replace($"{mainDir.FullName}", "")) + "/" :
                    Path.Combine(mainDir.Name, dir.FullName.Replace($"{mainDir.FullName}\\", "")) + "/";

                var fileNameByte = Encoding.UTF8.GetBytes(FileName);
                Stream.Write(BitConverter.GetBytes(fileNameByte.Length), 0, 4);
                Stream.Write(fileNameByte, 0, fileNameByte.Length);
            }
            var StreamFilePozition = Stream.Position;
            Stream.Position = 3;
            Stream.Write(BitConverter.GetBytes(StreamFilePozition), 0, 8);
            Stream.Position = StreamFilePozition;
        }

        public TDirectories[] GetTitleDirectories()
        {
            Stream.Position = 0;
            Stream.Seek(3, SeekOrigin.Current);
            Stream.Seek(8, SeekOrigin.Current);
            var bufer = new byte[4];
            Stream.Read(bufer, 0, bufer.Length);
            var DirCount = BitConverter.ToInt32(bufer, 0);
            var titleDirectories = new TDirectories[DirCount];
            for (var i = 0; i < DirCount; i++)
            {
                bufer = new byte[4];
                Stream.Read(bufer,0,4);
                var filePathLength = BitConverter.ToInt32(bufer, 0);
                bufer = new byte[filePathLength];
                Stream.Read(bufer, 0, filePathLength);
                var fileName = Encoding.UTF8.GetString(bufer);
                titleDirectories[i] = new TDirectories(filePathLength, fileName);
            }
            return titleDirectories;
        }


        //public TFile GetTitleFile(/*FileStream stream*/)
        //{
        //    var blockCount = 0;
        //    long positionInTheStream;
        //    var buffer = new byte[4];
        //    var blockLength = new long[0];
        //    Stream.Read(buffer);
        //    var filePathLength = BitConverter.ToInt32(buffer);
        //    buffer = new byte[filePathLength];
        //    Stream.Read(buffer);
        //    var fullName = Encoding.UTF8.GetString(buffer);
        //    buffer = new byte[8];
        //    Stream.Read(buffer);
        //    var fileLength = BitConverter.ToInt64(buffer);
        //    if (fileLength == 0)
        //    {
        //        Stream.Read(buffer, 0, 4);
        //        blockCount = BitConverter.ToInt32(buffer);
        //        Stream.Read(buffer);

        //        positionInTheStream = BitConverter.ToInt64(buffer);
        //        blockLength = new long[blockCount];

        //        for (int i = 0; i < blockCount; i++)
        //        {
        //            Stream.Read(buffer);
        //            blockLength[i] = BitConverter.ToInt32(buffer, 4);
        //            fileLength += blockLength[i];
        //            Stream.Seek(blockLength[i] - 8, SeekOrigin.Current);
        //        }
        //    }
        //    else
        //    {
        //        positionInTheStream = BitConverter.ToInt64(buffer);
        //        Stream.Seek(fileLength, SeekOrigin.Current);
        //    }


        //    return new TFile(filePathLength, fullName, fullName.Substring(fullName.LastIndexOf('\\') + 1), fileLength, positionInTheStream, blockCount, blockLength);
        //}
        public void AddTitleFile(DirectoryInfo mainDir, string fullName, long fileLenght, int blockCount = 0, bool isBiFile = false)
        {
            //var FileName = Path.Combine(mainDir.Name, fullName.Replace($"{mainDir.FullName}\\", ""));
            var fileNameByte = Encoding.UTF8.GetBytes(Path.Combine(mainDir.Name, fullName.Replace($"{mainDir.FullName}\\", "")));
            var FileLength = (isBiFile) ? 0 : fileLenght;
            Stream.Write(BitConverter.GetBytes(fileNameByte.Length), 0, 4);
            Stream.Write(fileNameByte, 0, fileNameByte.Length);
            Stream.Write(BitConverter.GetBytes(FileLength), 0, 8);
            if (FileLength == 0)
            {
                Stream.Write(BitConverter.GetBytes(blockCount), 0, 4);
            }
            Stream.Write(BitConverter.GetBytes(Stream.Position + 8), 0, 8);
        }


        public List<TFile> GetTitleFiles()
        {
            Stream.Position = 3;
            var buffer = new byte[8];
            Stream.Read(buffer, 0, 8);
            Stream.Position = BitConverter.ToInt64(buffer, 0);
            var titleFiles = new List<TFile>();
            while (Stream.Position < Stream.Length)
            {
                long positionInTheStream;
                var BlockCount = 0;
                var blockLength = new long[0];
                buffer = new byte[4];
                Stream.Read(buffer, 0, 4);
                var filePathLength = BitConverter.ToInt32(buffer, 0);
                buffer = new byte[filePathLength];
                Stream.Read(buffer, 0, buffer.Length);
                var fullName = Encoding.UTF8.GetString(buffer);
                buffer = new byte[8];
                Stream.Read(buffer, 0, 8);
                var fileLength = BitConverter.ToInt64(buffer, 0);
                if (fileLength == 0)
                {
                    Stream.Read(buffer, 0, 4);
                    BlockCount = BitConverter.ToInt32(buffer, 0);

                    Stream.Read(buffer, 0, 8);
                    positionInTheStream = BitConverter.ToInt64(buffer, 0);

                    blockLength = new long[BlockCount];
                    for (int i = 0; i < BlockCount; i++)
                    {
                        Stream.Read(buffer, 0, 8);
                        blockLength[i] = BitConverter.ToInt32(buffer, 4);
                        fileLength += blockLength[i];
                        Stream.Seek(blockLength[i] - 8, SeekOrigin.Current);
                    }
                }
                else
                {
                    Stream.Read(buffer, 0, 8);
                    positionInTheStream = BitConverter.ToInt64(buffer, 0);
                    Stream.Seek(fileLength, SeekOrigin.Current);
                }
                titleFiles.Add(new TFile(filePathLength, fullName, fullName.Substring(fullName.LastIndexOf('\\') + 1), fileLength, positionInTheStream, BlockCount, blockLength));
            }
            return titleFiles;
        }
    }
    internal class TFile
    {

        public int FilePathLength { get; }
        public string FullName { get; }
        public string Name { get; }
        public long FileLength { get; }
        public long PositionInTheStream { get; }
        public int BlockCount { get; }
        public long[] BlockLength { get; }


        public TFile(int filePathLength, string fulleName, string name, long fileLength, long positionInTheStream, int blockCount, long[] blockLength)
        {
            FilePathLength = filePathLength;
            FullName = fulleName;
            Name = name;
            FileLength = fileLength;
            PositionInTheStream = positionInTheStream;
            BlockCount = blockCount;
            BlockLength = blockLength;
        }

    }

    internal class TDirectories
    {
        public int FilePathLength { get;}
        public string FileName { get; }

        public TDirectories(int filePathLength, string fileName)
        {
            FilePathLength = filePathLength;
            FileName = fileName;
        }

    }
}
