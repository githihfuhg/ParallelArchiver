using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
            var directories =/* (oneFile) ? new List<DirectoryInfo>() :*/ mainDir.EnumerateDirectories("*", SearchOption.AllDirectories).ToList();
            directories.Add(mainDir);
            Stream.Write(Encoding.UTF8.GetBytes("dir"), 0, 3);
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
        //public void AddTitleFile(DirectoryInfo mainDir, string typeCompression ,bool IsCompressFile, string fullName,long fileLength = 0, int blockCount = 0/*,*//* bool bigFile = false*/)
        //{
        //    //var FileName = Path.Combine(mainDir.Name, fullName.Replace($"{mainDir.FullName}\\", ""));
        //    if (IsCompressFile)
        //    {
        //        Stream.Write(Encoding.UTF8.GetBytes("fil"), 0, 3);
        //        Stream.Write(BitConverter.GetBytes(Stream.Position + 8),0,8);
        //    }
        //    Stream.Write(Encoding.UTF8.GetBytes(typeCompression), 0, 2);
        //    var fileNameByte = Encoding.UTF8.GetBytes(Path.Combine(mainDir.Name,fullName.Replace($"{mainDir.FullName}\\", "")));
        //    var FileLength = fileLength;/*(fileLength==0) ? 0 : fileLength;*/
        //    Stream.Write(BitConverter.GetBytes(fileNameByte.Length), 0, 4);
        //    Stream.Write(fileNameByte, 0, fileNameByte.Length);
        //    Stream.Write(BitConverter.GetBytes(FileLength), 0, 8);
        //    if (fileLength == 0)
        //    {
        //        Stream.Write(BitConverter.GetBytes(blockCount), 0, 4);
        //    }
        //    Stream.Write(BitConverter.GetBytes(Stream.Position + 8), 0, 8);
        //}

        public void AddTitleFile(DirectoryInfo mainDir,bool IsCompressFile,TFile tFile)
        {
            //var FileName = Path.Combine(mainDir.Name, fullName.Replace($"{mainDir.FullName}\\", ""));
            if (IsCompressFile)
            {
                Stream.Write(Encoding.UTF8.GetBytes("fil"), 0, 3);
                Stream.Write(BitConverter.GetBytes(Stream.Position + 8), 0, 8);
            }
            Stream.Write(Encoding.UTF8.GetBytes(tFile.TypeСompression), 0, 2);
            var fileNameByte = Encoding.UTF8.GetBytes(Path.Combine(mainDir.Name, tFile.FullName.Replace($"{mainDir.FullName}\\", "")));
            Stream.Write(BitConverter.GetBytes(fileNameByte.Length), 0, 4);
            Stream.Write(fileNameByte, 0, fileNameByte.Length);
            Stream.Write(BitConverter.GetBytes(tFile.FileLength), 0, 8);
            if (tFile.FileLength == 0)
            {
                Stream.Write(BitConverter.GetBytes(tFile.BlockCount), 0, 4);
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
                var blockLength = new int[0];
                buffer = new byte[4];
                Stream.Read(buffer, 0, 2);
                var typeCompression = Encoding.UTF8.GetString(buffer,0,2);
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
                    blockLength = new int [BlockCount];
                    for (int i = 0; i < BlockCount; i++)
                    {

                        Stream.Read(buffer, 0, 4);
                        blockLength[i] = BitConverter.ToInt32(buffer, 0);
                        fileLength += blockLength[i];
                        Stream.Seek(blockLength[i] , SeekOrigin.Current);

                        //Stream.Read(buffer, 0, 8);
                        //blockLength[i] = BitConverter.ToInt32(buffer, 4);
                        //fileLength += blockLength[i];
                        //Stream.Seek(blockLength[i] - 8, SeekOrigin.Current);
                    }
                }
                else
                {
                    Stream.Read(buffer, 0, 8);
                    positionInTheStream = BitConverter.ToInt64(buffer, 0);
                    Stream.Seek(fileLength, SeekOrigin.Current);
                }
                titleFiles.Add(new TFile(typeCompression,filePathLength, fullName, fullName.Substring(fullName.LastIndexOf('\\') + 1), fileLength, positionInTheStream, BlockCount, blockLength));
            }
            return titleFiles;
        }
    }
    public class TFile
    {
        public string TypeСompression { get;}
        public int FilePathLength { get; }
        public string FullName { get; }
        public string Name { get; }
        public long FileLength { get; }
        public long PositionInTheStream { get; }
        public int BlockCount { get; }
        public int[] BlockLength { get; }


        internal TFile(string typeCompression,int filePathLength, string fullName, string name, long fileLength, long positionInTheStream, int blockCount, int[] blockLength)
        {
            TypeСompression = typeCompression;
            FilePathLength = filePathLength;
            FullName = fullName;
            Name = name;
            FileLength = fileLength;
            PositionInTheStream = positionInTheStream;
            BlockCount = blockCount;
            BlockLength = blockLength;
        }

        internal TFile(string typeCompression, long fileLength, string fullName)
        {
            TypeСompression = typeCompression;
            FullName = fullName;
            FileLength = fileLength;
            BlockCount = 0;
        }
        internal TFile(string typeCompression, string fullName, int blockCount)
        {
            TypeСompression = typeCompression;
            FileLength = 0;
            FullName = fullName;
            BlockCount = blockCount;
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
