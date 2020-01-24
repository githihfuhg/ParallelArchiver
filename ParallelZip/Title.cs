using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ParallelArhive
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
                Stream.Write(Encoding.UTF8.GetBytes("dir"),0,3);
                Stream.Write(BitConverter.GetBytes(DirCount),0,4);
                foreach (var dir in directories)
                {
                    FileName = (dir == mainDir) ?
                        Path.Combine(mainDir.Name, dir.FullName.Replace($"{mainDir.FullName}", "")) + "/" :
                        Path.Combine(mainDir.Name, dir.FullName.Replace($"{mainDir.FullName}\\", "")) + "/";

                    var fileNameByte = Encoding.UTF8.GetBytes(FileName);
                    FilePathLength = fileNameByte.Length;
                    Stream.Write(BitConverter.GetBytes(FilePathLength),0,4);
                    Stream.Write(fileNameByte,0,fileNameByte.Length);
                }
            }
            public void AddTitleFile(DirectoryInfo mainDir, string fullName, long fileLenght, int blockCount = 0)
            {
                FileName = Path.Combine(mainDir.Name, fullName.Replace($"{mainDir.FullName}\\", ""));
                var fileNameByte = Encoding.UTF8.GetBytes(FileName);
                FilePathLength = fileNameByte.Length;
                FileLength = (fileLenght >= 52428800) ? 0 : fileLenght;
                Stream.Write(BitConverter.GetBytes(FilePathLength),0,4);
                Stream.Write(fileNameByte,0,fileNameByte.Length);
                Stream.Write(BitConverter.GetBytes(FileLength),0,8);
                if (FileLength == 0)
                {
                    BlockCount = blockCount;
                    Stream.Write(BitConverter.GetBytes(BlockCount),0,4);
                }
                PositionInTheStream = Stream.Position;
                Stream.Write(BitConverter.GetBytes(Stream.Position + 8),0,8);
            }

            public Title[] GetTitleDirectories()
            {
                Stream.Seek(3, SeekOrigin.Current);
                var bufer = new byte[4];
                Stream.Read(bufer,0,bufer.Length);
                var DirCount = BitConverter.ToInt32(bufer,0);
                var titleDirectories = new Title[DirCount];
                for (var i = 0; i < DirCount; i++)
                {
                    try
                    {
                        bufer = new byte[4];
                        Stream.Read(bufer,0,bufer.Length);
                        FilePathLength = BitConverter.ToInt32(bufer,0);
                        bufer = new byte[FilePathLength];
                        Stream.Read(bufer, 0, FilePathLength);
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
                Stream.Read(buffer,0,buffer.Length);
                FilePathLength = BitConverter.ToInt32(buffer,0);
                buffer = new byte[FilePathLength];
                Stream.Read(buffer,0,buffer.Length);
                FileName = Encoding.UTF8.GetString(buffer);
                buffer = new byte[8];
                Stream.Read(buffer,0,buffer.Length);
                FileLength = BitConverter.ToInt64(buffer,0);
                if (FileLength == 0)
                {
                    Stream.Read(buffer, 0, 4);
                    BlockCount = BitConverter.ToInt32(buffer,0);
                }
                Stream.Read(buffer,0,buffer.Length);
                PositionInTheStream = BitConverter.ToInt64(buffer,0);

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
                    Stream.Read(buffer, 0, buffer.Length);
                    FilePathLength = BitConverter.ToInt32(buffer,0);
                    buffer = new byte[FilePathLength];
                    Stream.Read(buffer, 0, buffer.Length);
                    FileName = Encoding.UTF8.GetString(buffer);
                    buffer = new byte[8];
                    Stream.Read(buffer, 0, buffer.Length);
                    FileLength = BitConverter.ToInt64(buffer,0);
                    if (FileLength == 0)
                    {
                        Stream.Read(buffer, 0, 4);
                        BlockCount = BitConverter.ToInt32(buffer,0);
                    }

                    Stream.Read(buffer, 0, buffer.Length);
                    PositionInTheStream = BitConverter.ToInt64(buffer,0);

                    if (FileLength == 0)
                    {
                        for (int i = 0; i < BlockCount; i++)
                        {
                            var Buffer = new byte[8];
                            Stream.Read(Buffer, 0, Buffer.Length);
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
