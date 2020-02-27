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

    internal class CompressArchive
    {
        private ParallelArchiverEvents ParallelArchEvents { get;}
        private int NumberOfCores { get; }
        public int DegreeOfParallelism { get; set; } = 45;
        private Title Title { get; set; }
        private DirectoryInfo MainDir { get; set; }
        private FileStream ResultStream { get; set; }
        private PqzCompressionLevel CompressL { get; set; }
        private bool MaximumTxtCompression{ get; set; }
        private bool IsCompressFile { get; set; }


        internal CompressArchive(ParallelArchiverEvents parallelArchiverEvents,
            PqzCompressionLevel CompressL, bool MaximumTxtCompression)
        {
            ParallelArchEvents = parallelArchiverEvents;
            NumberOfCores = Environment.ProcessorCount;
        }

        public void CompressFile(string input, string result)
        {
            using (FileStream resultStream = File.Create(result))
            {
                FileInfo fileInfo = new FileInfo(input);
                MainDir = fileInfo.Directory;
                ResultStream = resultStream;
                Title = new Title(resultStream);
                IsCompressFile = true;
                AddFile(fileInfo);

            }
            GC.Collect();
        }

        public void CompressDirectory(string inputDir, string outputDir)
        {

            var timer = new Stopwatch();
            timer.Start();
            MainDir = new DirectoryInfo(inputDir);
            using (FileStream resultStream = File.Create(outputDir))
            {
                ResultStream = resultStream;
                Title = new Title(resultStream);
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


            ParallelArchEvents.Start(pathFile);
            //NumberOfFiles = pathFile.Length;
            foreach (var file in pathFile)
            {
                if (file.Length >= 5242880)
                {
                    CompressBigFile(file);
                }
                else
                {
                    SmallFile.Add(file);
                }
            }
            CompressSmallFile(SmallFile);
            ParallelArchEvents.Restart();
            timer.Stop();
            var time = timer.ElapsedMilliseconds;
        }

        private void CompressBigFile(FileInfo fileI)
        {
            var sizeBlock = BalancingBlocks(fileI.Length, SetDegreeOfParallelism(fileI.Length));
            var blockCount = DegreeOfParallelism * NumberOfCores;
            var typeCompression = TypeCompression(fileI.Name);

            Title.AddTitleFile(MainDir, IsCompressFile, new TFile(typeCompression, fileI.FullName, blockCount));

            using (var read = fileI.OpenRead())
            {
                for (int i = 0; i < DegreeOfParallelism; i++)
                {
                    var data = BalancingBlocks(sizeBlock[i], NumberOfCores).Select(p =>
                    {
                        var bytes = new byte[p];
                        read.Read(bytes, 0, bytes.Length);
                        ParallelArchEvents.AddProgressFile(fileI.Name, bytes.Length, read.Length, read.Position);
                        return bytes;

                    }).Select(x => Task.Run(() => CompressBlock(x,typeCompression))).ToArray();

                    Task.WaitAll(data);
                    WriteFile(data);
                }
            }
        }
        private void WriteFile(Task<byte[]>[] data)
        {
            var resultDate = data.Select(r => r.Result).ToArray();
            foreach (var res in resultDate)
            {
                ResultStream.Write(BitConverter.GetBytes(res.Length), 0, 4);
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
                var typeCompression = TypeCompression(file.Name);
                var CompressFile = CompressBlock(buffer, /*"gz"*/typeCompression);
               
                lock (ResultStream)
                {
                    Title.AddTitleFile(MainDir, IsCompressFile, new TFile(/*"gz"*/typeCompression, CompressFile.Length, file.FullName));
                    ResultStream.Write(CompressFile, 0, CompressFile.Length);
                    ParallelArchEvents.AddProgressFile(file.Name, file.Length);
                }

            })).ToArray();

            Task.WaitAll(tasks);
        }

        private byte[] CompressBlock(byte[] data, string typeCompression)
        {

            using (var compressedStream = new MemoryStream())
            {
                if (typeCompression == "gz")
                {
                    using (var zipStream = new GZipStream(compressedStream, (CompressionLevel)CompressL))
                    {
                        zipStream.Write(data, 0, data.Length);
                        zipStream.Close();
                        return compressedStream.ToArray();
                    }
                }
                else
                {
                    var соmpressL = (MaximumTxtCompression) ? CompressL : PqzCompressionLevel.Fastest;
                    using (var brStream = new BrotliStream(compressedStream,(CompressionLevel)соmpressL))
                    {
                        brStream.Write(data, 0, data.Length);

                        brStream.Close();
                        return compressedStream.ToArray();
                    }
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

        private string TypeCompression(string Name)
        {
            return Extension.Contains(Path.GetExtension(Name)) ? "br" : "gz";
        }


        private string[] Extension =
        {
            ".txt",".text",".cpp",".c",".cs",".py",".css",".html",
            ".xml",".json",".text","rtf",".html",".xml",".config",".h"
        };


    }
}
