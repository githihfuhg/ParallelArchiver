using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace test
{
    public class CompressArchive : ParallelArchiverEvents
    {
        private readonly int NumberOfCores = Environment.ProcessorCount;
        private long Counter = 0;
        private long NumberOfFiles = 0;
        public int DegreeOfParallelism = 45;
        private ParallelArchiverEvents PAEvents;

        //private event Action<string, int, int> Progress;

        //public CompressArchive(ParallelArchiverEvents paEvent)
        //{
        //    PAEvents = paEvent;
        //}


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

            CompressSmallFile(SmallFile, mainDir, create, compressL);
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
                            //Progress?.Invoke(read.Name, progressFile, fullProgress);
                            AddProgressFile(read.Name, progressFile, fullProgress);
                        }
                        else
                        {
                            //Progress?.Invoke(read.Name, progressFile, progressFile);
                          AddProgressFile(read.Name, progressFile, progressFile);
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
        private void CompressSmallFile(List<FileInfo> fileInfo, DirectoryInfo mainDir, FileStream create, PqzCompressionLevel compressL)
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
                    //PAEvents.Progress?.Invoke(file.Name, 100, fullProgress);
                    AddProgressFile(file.Name, 100, fullProgress);
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
        private byte[] CompressBlock(byte[] data, PqzCompressionLevel copressL)
        {
            using var compressedStream = new MemoryStream();
            using var zipStream = new GZipStream(compressedStream, (CompressionLevel)copressL);
            zipStream.Write(data, 0, data.Length);
            zipStream.Close();
            return compressedStream.ToArray();
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
        private int SetDegreeOfParallelism(long FileLength, int portionLenght = 4718592)
        {
            var result = (float)(FileLength / NumberOfCores) / portionLenght;
            return DegreeOfParallelism = (result % 10 == 0 || result < 1) ? (int)result + 1 : (int)result;
        }

    }
}
