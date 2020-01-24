using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace test
{
    internal class Title
    {
        public int FilePathLength { get; private set; } = 0;
        public string FileName { get; private set; } = "";
        public long FileLength { get; private set; } = 0;
        public long PositionInTheStream { get; private set; } = 0;
        public int BlockCount { get; private set; } = 0;


        private FileStream Stream;
        public Title(FileStream stream)
        {
            Stream = stream;
        }
        private Title() { }
        private Title(Title title)
        {
            FilePathLength = title.FilePathLength;
            FileName = title.FileName;
            FileLength = title.FileLength;
            PositionInTheStream = title.PositionInTheStream;
            BlockCount = title.BlockCount;
        }

        public void AddTitleDirectories(DirectoryInfo mainDir)
        {
            var directories = mainDir.EnumerateDirectories("*", SearchOption.AllDirectories).ToList();
            directories.Add(mainDir);
            int DirCount = directories.Count();
            Stream.Write(Encoding.UTF8.GetBytes("dir"));
            Stream.Write(BitConverter.GetBytes(DirCount));
            foreach (var dir in directories)
            {
                FileName = (dir == mainDir) ? 
                    Path.Combine(mainDir.Name, dir.FullName.Replace($"{mainDir.FullName}", "")) + "/":
                    Path.Combine(mainDir.Name, dir.FullName.Replace($"{mainDir.FullName}\\", "")) + "/";

                var fileNameByte = Encoding.UTF8.GetBytes(FileName);
                FilePathLength = fileNameByte.Length;
                Stream.Write(BitConverter.GetBytes(FilePathLength));
                Stream.Write(fileNameByte);
            }
        }
        public void AddTitleFile(DirectoryInfo mainDir, string fullName, long fileLenght, int blockCount = 0)
        {
            FileName = Path.Combine(mainDir.Name, fullName.Replace($"{mainDir.FullName}\\", ""));
            var fileNameByte = Encoding.UTF8.GetBytes(FileName);
            FilePathLength = fileNameByte.Length;
            FileLength = (fileLenght >= 52428800) ? 0 : fileLenght;
            Stream.Write(BitConverter.GetBytes(FilePathLength));
            Stream.Write(fileNameByte);
            Stream.Write(BitConverter.GetBytes(FileLength));
            if (FileLength == 0)
            {
                BlockCount = blockCount;
                Stream.Write(BitConverter.GetBytes(BlockCount));
            }
            PositionInTheStream = Stream.Position;
            Stream.Write(BitConverter.GetBytes(Stream.Position + 8));
        }

        public Title[] GetTitleDirectories()
        {
            Stream.Seek(3, SeekOrigin.Current);
            var bufer = new byte[4];
            Stream.Read(bufer);
            var DirCount = BitConverter.ToInt32(bufer);
            var titleDirectories = new Title[DirCount];
            for (var i = 0; i < DirCount; i++)
            {
                try
                {
                    bufer = new byte[4];
                    Stream.Read(bufer);
                    FilePathLength = BitConverter.ToInt32(bufer);
                    bufer = new byte[FilePathLength];
                    Stream.Read(bufer,0,FilePathLength);
                    FileName = Encoding.UTF8.GetString(bufer);
                    titleDirectories[i] = new Title(this);
                }
                catch
                {
                    var a = 1;
                }/* new Title(){FilePathLength = FilePathLength,FileName = FileName};*/
            }
            return titleDirectories;
        }

        public Title GetTitleFile(/*FileStream stream*/)
        {
            BlockCount = 0;
            var buffer = new byte[4];
            Stream.Read(buffer);
            FilePathLength = BitConverter.ToInt32(buffer);
            buffer = new byte[FilePathLength];
            Stream.Read(buffer);
            FileName = Encoding.UTF8.GetString(buffer);
            buffer = new byte[8];
            Stream.Read(buffer);
            FileLength = BitConverter.ToInt64(buffer);
            if (FileLength == 0)
            {
                Stream.Read(buffer, 0, 4);
                BlockCount = BitConverter.ToInt32(buffer);
            }
            Stream.Read(buffer);
            PositionInTheStream = BitConverter.ToInt64(buffer);

            return this;
        }

        public Dictionary<string, Title> GetTitleFiles()
        {
            var titleFiles = new Dictionary<string, Title>();

            //413867339
            //830516399
            while (Stream.Position < Stream.Length)
            {
                BlockCount = 0;
                var buffer = new byte[4];
                Stream.Read(buffer);
                FilePathLength = BitConverter.ToInt32(buffer);
                buffer = new byte[FilePathLength];
                Stream.Read(buffer);
                FileName = Encoding.UTF8.GetString(buffer);
                buffer = new byte[8];
                Stream.Read(buffer);
                FileLength = BitConverter.ToInt64(buffer);
                if (FileLength == 0)
                {
                    Stream.Read(buffer, 0, 4);
                    BlockCount = BitConverter.ToInt32(buffer);
                }

                Stream.Read(buffer);
                PositionInTheStream = BitConverter.ToInt64(buffer);

                if (FileLength == 0)
                {
                    for (int i = 0; i < BlockCount; i++)
                    {
                        var Buffer = new byte[8];
                        Stream.Read(Buffer);
                        var blockLength = BitConverter.ToInt32(Buffer, 4);
                        Stream.Seek(blockLength - 8, SeekOrigin.Current);
                    }

                }
                else
                {
                    Stream.Seek(FileLength, SeekOrigin.Current);
                }


                titleFiles[FileName] = new Title(this);
            }
            //Stream.Seek(-Stream.Length, SeekOrigin.Current);
            Stream.Position = 0;
            return titleFiles;
        }


    }
}
