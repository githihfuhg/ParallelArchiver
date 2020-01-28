using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test
{
    internal class DecompressArchive
    {
        private ParallelArchiverEvents PAEvents;
        //public DecompressArchive(ParallelArchiverEvents paEvents)
        //{
        //    PAEvents = paEvents;
        //}
        public void Decompress(string inputFile, string outputDir, IEnumerable<string> fileExtension = null, IEnumerable<string> fileName = null)
        {
            var timer = new Stopwatch();
            timer.Start();
            var fileInfo = new FileInfo(inputFile);
            using (var read = fileInfo.OpenRead())
            {
                if (Type(read) == "dir")
                {
                    CreateDir(read, outputDir);
                    //ParallelCreateFiles(read, outputDir);
                    ParallelCreateFiles(read, outputDir, new[] { ".txt", ".docx" });
                    //CreateFiles(read, outputDir);
                }
                else
                {
                    DecompressOneFile(read, outputDir);
                }

            }

            GC.Collect();
            timer.Stop();
            var time = timer.ElapsedMilliseconds;

        }
        private void DecompressOneFile(FileStream read, string outputDir)
        {
            var outputFileName = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(read.Name));
            using (FileStream create = File.Create(outputFileName))
            {
                while (read.Position < read.Length)
                {
                    //DecompressBlock(read, create);
                    DecompressBigFile(read, create);
                }
            }
        }
       
        public void ParallelCreateFiles(FileStream read, string outputDir, IEnumerable<string> fileName = null, IEnumerable<string> fileExtension = null)
        {
            var title = new Title(read);
            var titles = new List<TFile>();
            var allTitles = title.GetTitleFiles();
            var timer = new Stopwatch();
            timer.Start();

            if (fileName != null)
            {
                titles = fileName.SelectMany(file => allTitles.Where(title =>
                    title.FileName.Contains(file))).ToList();
            }

            if (fileExtension != null)
            {
                titles.AddRange(fileExtension.SelectMany(ext =>
                    allTitles.Where(title =>
                        title.FileName.Substring(title.FileName.Length - ext.Length) == ext && !titles.Contains(title))));
            }

            if (fileExtension == null && fileName == null)
            {
                titles = allTitles;
            }

            //titles = (fileExtension == null && fileName == null) ? allTitles :
            //    (fileName != null) ? titles = fileName.SelectMany(file => allTitles.Where(title =>
            //        title.FileName.Contains(file))).ToList() : fileExtension.SelectMany(ext =>
            //        allTitles.Where(title =>
            //            title.FileName.Substring(title.FileName.Length - ext.Length) == ext &&
            //            !titles.Contains(title))).ToList();

            //titles = fileName?.SelectMany(file => t.Where(title =>
            //    title.FileName.Contains(file))).ToList() ?? titles;


            var task = titles.Select(t => Task.Run(() =>
            {
                var fullDir = Path.Combine(outputDir, t.FileName);
                using (FileStream create = File.Open(fullDir, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    if (t.BlockCount != 0)
                    {
                        DecompressBigFile(read, create, t.PositionInTheStream, t.BlockCount);
                    }
                    else
                    {
                        DecompressSmallFile(read, create, t);
                    }
                }
            })).ToArray();
            Task.WaitAll(task);

        }
        private void DecompressBigFile(FileStream read, FileStream create, long PositionInTheStream = 0, int blockCount = 0)
        {
            lock (read)
            {
                byte[] data;
                long counter = 0;
                long stop = (blockCount != 0) ? blockCount : read.Length;
                read.Position = PositionInTheStream;
                while (counter < stop)
                {
                    var Buffer = new byte[8];
                    read.Read(Buffer);
                    var blockLength = BitConverter.ToInt32(Buffer, 4);
                    data = new byte[blockLength];
                    Buffer.CopyTo(data, 0);
                    read.Read(data, 8, data.Length - 8);
                    using (var decompressedStream = new MemoryStream(data))
                    {
                        using var resultStream = new GZipStream(decompressedStream, CompressionMode.Decompress);
                        lock (create)
                        {
                            resultStream.CopyTo(create);
                        }
                    }
                    counter = (blockCount != 0) ? counter + 1 : read.Position;
                }
            }
        }
        //private void DecompressBlock(FileStream read, FileStream create, long fileLength = 0)
        //{
        //    byte[] data;
        //    if (fileLength == 0)
        //    {
        //        var Buffer = new byte[8];
        //        read.Read(Buffer);
        //        var blockLength = BitConverter.ToInt32(Buffer, 4);
        //        data = new byte[blockLength];
        //        Buffer.CopyTo(data, 0);
        //        read.Read(data, 8, data.Length - 8);
        //    }
        //    else
        //    {
        //        data = new byte[fileLength];
        //        read.Read(data);
        //    }
        //    using var decompressedStream = new MemoryStream(data);
        //    using var resultStream = new GZipStream(decompressedStream, CompressionMode.Decompress);
        //    resultStream.CopyTo(create);

        //}



        private void DecompressSmallFile(FileStream read, FileStream create, TFile title)
        {
            var data = new byte[title.FileLength];
            lock (read)
            {
                read.Position = title.PositionInTheStream;
                read.Read(data);
            }
            using var decompressedStream = new MemoryStream(data);
            using var resultStream = new GZipStream(decompressedStream, CompressionMode.Decompress);
            lock (create)
            {
                resultStream.CopyTo(create);
            }

        }


        private void CreateDir(FileStream read, string outputDir)
        {
            var timer = new Stopwatch();
            timer.Start();
            foreach (var t in new Title(read).GetTitleDirectories())
            {
                var fullDir = Path.Combine(outputDir, t.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(fullDir));
            }
            timer.Stop();
            var time = timer.ElapsedMilliseconds;

        }
        private string Type(FileStream stream)
        {
            string type;
            var buffer = new byte[3];
            stream.Read(buffer);
            stream.Seek(-3, SeekOrigin.Current);

            try
            {
                type = Encoding.UTF8.GetString(buffer);
            }
            catch
            {
                type = "";
            }

            return type;
        }


    }
}
