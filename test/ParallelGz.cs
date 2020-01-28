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
    public enum PqzCompressionLevel
    {
        Optimal,
        Fastest,
        NoCompression,
    }

    public class ParallelGz
    {
        private readonly int NumberOfCores = Environment.ProcessorCount;
        private long Counter = 0;
        private long NumberOfFiles = 0;
        public int DegreeOfParallelism = 45;

        public event Action<string, int, int> Progress;
        public void CompressFile(string input, string result, PqzCompressionLevel compressL)
        {
            using (FileStream create = File.Create(result))
            {
                CompressBigFile(new FileInfo(input), create, compressL);
            }
            GC.Collect();

        }

        public void CompressDirectory(string inputDir, string outputDir, PqzCompressionLevel compressL)
        {

            var timer = new Stopwatch();
            timer.Start();
            var directoryInfo = new DirectoryInfo(inputDir);
            using (FileStream resultStream = File.Create(outputDir))
            {
                var title = new Title(resultStream);
                title.AddTitleDirectories(directoryInfo/*, resultStream*/);
                AddFile(directoryInfo, resultStream, compressL);
                resultStream.Dispose();
            }
            GC.Collect();
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

        public void Decompress(string inputFile, string outputDir, IEnumerable<string> fileName = null, IEnumerable<string> fileExtension = null)
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
                    DecompressBlock(read, create);
                }
            }
        }

        public void ParallelCreateFiles(FileStream read, string outputDir, IEnumerable<string> fileName = null, IEnumerable<string> fileExtension = null)
        {
            var title = new Title(read);
            var  titles = new List<TFile>();
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

        private void DecompressBigFile(FileStream read, FileStream create, long PositionInTheStream, int blockCount)
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

        //private void CreateFiles(FileStream read, string outputDir)
        //{
        //    var title = new Title(read);

        //    while (read.Position < read.Length)
        //    {
        //        title = title.GetTitleFile(/*read*/);
        //        var fullDir = Path.Combine(outputDir, title.FileName);
        //        //using (FileStream create = File.Create(fullDir))
        //        //{
        //        using (FileStream create = File.Open(fullDir, FileMode.OpenOrCreate, FileAccess.Write))
        //        {
        //            if (title.BlockCount != 0)
        //            {
        //                for (int i = 0; i < title.BlockCount; i++)
        //                {
        //                    DecompressBlock(read, create);
        //                }

        //            }
        //            else
        //            {
        //                DecompressBlock(read, create, title.FileLength);
        //            }
        //        }

        //    }

        //}
        private void CompressBigFile(FileInfo fileInfo, FileStream create, PqzCompressionLevel compressL)
        {
            var sizeBlock = BalancingBlocks(fileInfo.Length, SetDegreeOfParallelism(fileInfo.Length));
            using (var read = fileInfo.OpenRead())
            {

                for (int i = 0; i < DegreeOfParallelism; i++)
                {
                    var date = BalancingBlocks(sizeBlock[i], NumberOfCores).Select(p =>
                             {
                                 var bytes = new byte[p];
                                 read.Read(bytes, 0, bytes.Length);
                                 var progressFile = (int)(read.Position * 100 / read.Length);
                                 if (NumberOfFiles > 1)
                                 {
                                     var fullProgress = (int)(Counter * 100 / NumberOfFiles);
                                     Progress?.Invoke(read.Name, progressFile, fullProgress);
                                 }
                                 else
                                 {
                                     Progress?.Invoke(read.Name, progressFile, progressFile);
                                 }
                                 return bytes;

                             }).Select(x => Task.Factory.StartNew(() =>
                         CompressBlock(x, compressL), TaskCreationOptions.LongRunning)).ToArray();

                    Task.WaitAll(date);
                    var resultDate = date.Select(r => r.Result).ToArray();
                    //Progress?.Invoke(read.Position, read.Length);
                    foreach (var res in resultDate)
                    {
                        var cBlockSize = BitConverter.GetBytes(res.Length);
                        for (int ind = 0; ind < 4; ind++)
                        {
                            res[ind + 4] = cBlockSize[ind]; // костыль
                        }

                        create.Write(res, 0, res.Length);
                    }
                }
            }
        }


        private byte[] CompressBlock(byte[] data, PqzCompressionLevel copressL)
        {
            using var compressedStream = new MemoryStream();
            using var zipStream = new GZipStream(compressedStream, (CompressionLevel)copressL);
            zipStream.Write(data, 0, data.Length);
            zipStream.Close();
            return compressedStream.ToArray();
        }

        private void DecompressBlock(FileStream read, FileStream create, long fileLength = 0)
        {
            byte[] data;
            if (fileLength == 0)
            {
                var Buffer = new byte[8];
                read.Read(Buffer);
                var blockLength = BitConverter.ToInt32(Buffer, 4);
                data = new byte[blockLength];
                Buffer.CopyTo(data, 0);
                read.Read(data, 8, data.Length - 8);
            }
            else
            {
                data = new byte[fileLength];
                read.Read(data);
            }
            using var decompressedStream = new MemoryStream(data);
            using var resultStream = new GZipStream(decompressedStream, CompressionMode.Decompress);
            resultStream.CopyTo(create);

        }

        private long[] BalancingBlocks(long fileLength, int blockCount)
        {

            var blocks = Enumerable.Range(0, blockCount).Select(x => fileLength / blockCount).ToArray();
            var Sum = blocks.Sum();
            if (Sum != fileLength)
            {
                blocks[^1] += fileLength - blocks.Sum();
            }

            return blocks;
        }




        private void AddSmallFile(List<FileInfo> fileInfo, DirectoryInfo mainDir, FileStream create, PqzCompressionLevel compressL)
        {
            var timer = new Stopwatch();
            timer.Start();
            var title = new Title(create);


            var tasks = fileInfo.Select(file => Task.Run(() =>
            {
                var Bufer = new byte[file.Length];

                using (FileStream readFile = file.Open(FileMode.Open, FileAccess.Read))
                {
                    readFile.Read(Bufer);
                }

                var CompressFile = CompressBlock(Bufer, compressL);

                lock (create)
                {
                    title.AddTitleFile(mainDir, file.FullName, CompressFile.Length);
                    create.Write(CompressFile);
                    var fullProgress = (int)(Counter * 100 / NumberOfFiles);
                    Progress?.Invoke(file.Name, 100, fullProgress);
                    Counter++;
                }

            })).ToArray();

            Task.WaitAll(tasks);

            //var CompressFiles = fileInfo.Select(file => Task.Run(() =>
            //{
            //    var Bufer = new byte[file.Length];
            //    using (var readFile = file.OpenRead())
            //    {
            //        readFile.Read(Bufer);
            //    }
            //    return CompressBlock(Bufer, compressL);
            //})).ToArray();

            //Task.WaitAll(CompressFiles);

            //timer.Stop();
            //var time = timer.ElapsedMilliseconds;
            //for (int i = 0; i < CompressFiles.Length; i++)
            //{
            //    title.AddTitleFile(mainDir,fileInfo[i].FullName, CompressFiles[i].Result.Length);
            //    create.Write(CompressFiles[i].Result);
            //}

        }

        private void AddFile(DirectoryInfo mainDir, FileStream create, PqzCompressionLevel compressL)
        {
            var timer = new Stopwatch();
            timer.Start();
            List<FileInfo> SmallFile = new List<FileInfo>();
            var pathFile = mainDir.EnumerateFiles("*", SearchOption.AllDirectories).ToArray();
            var title = new Title(create);
            NumberOfFiles = pathFile.Length;
            foreach (var file in pathFile)
            {
                if (file.Length >= 52428800)
                {
                    var bocksCount = SetDegreeOfParallelism(file.Length) * NumberOfCores;
                    title.AddTitleFile(mainDir, file.FullName, file.Length, bocksCount);
                    CompressBigFile(file, create, compressL);
                    Counter++;
                }
                else
                {
                    SmallFile.Add(file);
                }
            }

            AddSmallFile(SmallFile, mainDir, create, compressL);
            //SetDegreeOfParallelism(SmallFile.Count, 30);
            //DegreeOfParallelism = 1;
            //var sizeBlock = (int)Math.Ceiling(SmallFile.Count / (double)NumberOfCores) / DegreeOfParallelism;
            //Enumerable.Range(0, NumberOfCores * DegreeOfParallelism).Select(i => SmallFile
            //        .Skip(i * sizeBlock).Take(sizeBlock).ToList()).ToList()
            //    .ForEach(files => { AddSmallFile(files, mainDir, create, compressL); });
            //timer.Stop();
            NumberOfFiles = 0;
            Counter = 0;
            var time = timer.ElapsedMilliseconds;
        }
       

        private int SetDegreeOfParallelism(long FileLength, int portionLenght = 4718592)
        {
            var result = (float)(FileLength / NumberOfCores) / portionLenght;
            return DegreeOfParallelism = (result % 10 == 0 || result < 1) ? (int)result + 1 : (int)result;
        }

    }
}
