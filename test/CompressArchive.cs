using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace test
{
    public enum PqzCompressionLevel
    {
        Optimal,
        Fastest,
        NoCompression,
    }

    internal class CompressArchive : ParallelArchiverEvents
    {
        private int NumberOfCores { get; } = Environment.ProcessorCount;
        public int DegreeOfParallelism { get; set; } = 45;
        private Title Title { get; set; }
        private DirectoryInfo MainDir { get; set; }
        private FileStream ResultStream { get; set; }
        private PqzCompressionLevel CompressL { get; set; }
        private bool StrongTxtCompression { get; set; }
        private bool IsCompressFile { get; set; }

        public void CompressFile(string input, string result, PqzCompressionLevel compressL)
        {
            using (FileStream resultStream = File.Create(result))
            {
                FileInfo fileInfo = new FileInfo(input);
                MainDir = fileInfo.Directory;
                ResultStream = resultStream;
                Title = new Title(resultStream);
                CompressL = compressL;
                IsCompressFile = true;
                AddFile(fileInfo);

            }
            GC.Collect();
        }

        public void CompressDirectory(string inputDir, string outputDir, PqzCompressionLevel compressL)
        {

            var timer = new Stopwatch();
            timer.Start();
            MainDir = new DirectoryInfo(inputDir);
            using (FileStream resultStream = File.Create(outputDir))
            {
                ResultStream = resultStream;
                Title = new Title(resultStream);
                CompressL = compressL;
                IsCompressFile = false;
                Title.AddTitleDirectories(MainDir);
                AddFile();
            }
            GC.Collect();
            timer.Stop();
            var time = timer.ElapsedMilliseconds;
        }
        private void AddFile(FileInfo fileInfo = null)
        {
            var timer = new Stopwatch();
            timer.Start();
            List<FileInfo> SmallFile = new List<FileInfo>();
            FileInfo[] pathFile = (!IsCompressFile) ?
                MainDir.EnumerateFiles("*", SearchOption.AllDirectories).ToArray() :
                new[] { fileInfo };


            Start(pathFile);
            //NumberOfFiles = pathFile.Length;
            foreach (var file in pathFile)
            {
                if (file.Length >= 10485760)
                {
                    CompressBigFile(file);
                }
                else
                {
                    SmallFile.Add(file);
                }
            }
            CompressSmallFile(SmallFile);
            Restart();
            timer.Stop();
            var time = timer.ElapsedMilliseconds;
        }

        private void CompressBigFile(FileInfo fileInfo)
        {
            var sizeBlock = BalancingBlocks(fileInfo.Length, SetDegreeOfParallelism(fileInfo.Length));
            Title.AddTitleFile(MainDir, fileInfo.FullName, fileInfo.Length, IsCompressFile, DegreeOfParallelism * NumberOfCores, true);


            using (var read = fileInfo.OpenRead())
            {
                for (int i = 0; i < DegreeOfParallelism; i++)
                {
                    var date = BalancingBlocks(sizeBlock[i], NumberOfCores).Select(p =>
                    {
                        var bytes = new byte[p];
                        read.Read(bytes, 0, bytes.Length);
                        AddProgressFile(fileInfo.Name, bytes.Length, read.Length, read.Position);
                        return bytes;

                    }).Select(x => Task.Run(() => GzCompressByte(x))).ToArray();

                    Task.WaitAll(date);
                    WriteFile(date);
                }
            }
        }
        private void WriteFile(Task<byte[]>[] data)
        {
            var resultDate = data.Select(r => r.Result).ToArray();
            foreach (var res in resultDate)
            {
                var cBlockSize = BitConverter.GetBytes(res.Length);
                for (int ind = 0; ind < 4; ind++)
                {
                    res[ind + 4] = cBlockSize[ind]; // костыль
                }

                ResultStream.Write(res, 0, res.Length);
            }
        }



        private void CompressSmallFile(List<FileInfo> fileInfo)
        {
            var timer = new Stopwatch();
            timer.Start();
            var tasks = fileInfo.Select(file => Task.Run(() =>
            {
                var buffer = new byte[file.Length];

                using (FileStream readFile = file.Open(FileMode.Open, FileAccess.Read))
                {
                    readFile.Read(buffer, 0, buffer.Length);
                }

                var CompressFile = GzCompressByte(buffer);

                lock (ResultStream)
                {
                    Title.AddTitleFile(MainDir, file.FullName, CompressFile.Length,IsCompressFile);
                    ResultStream.Write(CompressFile, 0, CompressFile.Length);
                    AddProgressFile(file.Name, file.Length);
                }

            })).ToArray();

            Task.WaitAll(tasks);
        }

        private byte[] BrotliCompressByte(byte[] data)
        {

            using (var compressedStream = new MemoryStream())
            {
                using (var brStream = new BrotliStream(compressedStream, (CompressionLevel)CompressL))
                {
                    brStream.Write(data, 0, data.Length);
                    brStream.Close();
                    return compressedStream.ToArray();
                }
            }
        }

        private byte[] GzCompressByte(byte[] data)
        {

            using (var compressedStream = new MemoryStream())
            {
                using (var zipStream = new GZipStream(compressedStream, (CompressionLevel)CompressL))
                {
                    zipStream.Write(data, 0, data.Length);
                    zipStream.Close();
                    return compressedStream.ToArray();
                }
            }
        }





        private long[] BalancingBlocks(long fileLength, int blockCount)
        {
            var blocks = Enumerable.Range(0, blockCount).Select(x => fileLength / blockCount).ToArray();
            var Sum = blocks.Sum();
            if (Sum != fileLength)
            {
                blocks[blocks.Length - 1] += fileLength - blocks.Sum();
            }
            return blocks;
        }
        private int SetDegreeOfParallelism(long FileLength, int portionLength = 4718592)  /*4718592*/
        {
            var result = (float)(FileLength / NumberOfCores) / portionLength;
            return DegreeOfParallelism = (result % 10 == 0 || result < 1) ? (int)result + 1 : (int)result;
        }
        

    }
}
