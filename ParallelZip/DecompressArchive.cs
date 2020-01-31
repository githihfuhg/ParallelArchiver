using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelZip
{
    internal class DecompressArchive : ParallelArchiverEvents
    {
        private FileStream Read;
        private string OutputDir;
        private Title Title;
        public void Decompress(string inputFile, string outputDir, IEnumerable<string> fileExtension = null, IEnumerable<string> fileName = null)
        {
            var timer = new Stopwatch();
            timer.Start();
            var fileInfo = new FileInfo(inputFile);
            using (var read = fileInfo.OpenRead())
            {
                Read = read;
                OutputDir = outputDir;
                Title = new Title(read);
                if (IsDir())
                {
                    CreateDir();
                    ParallelCreateFiles(false, fileExtension, fileName);

                }
                else
                {
                    ParallelCreateFiles();
                }
            }

            GC.Collect();
            timer.Stop();
            var time = timer.ElapsedMilliseconds;

        }

        private void ParallelCreateFiles(bool isOneFiel = true, IEnumerable<string> fileName = null, IEnumerable<string> fileExtension = null)
        {
            var titles = new List<TFile>();
            var allTitles = Title.GetTitleFiles();
            var timer = new Stopwatch();
            timer.Start();
            if (fileName != null)
            {
                titles = fileName.SelectMany(file => allTitles.Where(title =>
                    title.FullName.Contains(file))).ToList();
            }

            if (fileExtension != null)
            {
                titles.AddRange(fileExtension.SelectMany(ext =>
                    allTitles.Where(title =>
                        title.FullName.Substring(title.FullName.Length - ext.Length) == ext && !titles.Contains(title))));
            }

            if (fileExtension == null && fileName == null)
            {
                titles = allTitles;
            }
            Start(titles);
            var task = titles.Select(t => Task.Run(() =>
            {
                var fullDir = (isOneFiel) ? Path.Combine(OutputDir, t.Name) : Path.Combine(OutputDir, t.FullName);
                using (FileStream create = File.Open(fullDir, FileMode.OpenOrCreate, FileAccess.Write))
                {

                    if (t.BlockCount != 0)
                    {
                        DecompressBigFile(create, t);
                    }
                    else
                    {
                        DecompressSmallFile(create, t);
                    }

                }

            })).ToArray();
            Task.WaitAll(task);
            Restart();

        }

        private void DecompressBigFile(FileStream create, TFile tfile)
        {
            byte[] data;
            var pozition = tfile.PositionInTheStream;
            for (long i = 0, pozEvent = 0; i < tfile.BlockCount; i++)
            {
                lock (Read)
                {
                    Read.Position = pozition;
                    data = new byte[tfile.BlockLength[i]];
                    Read.Read(data, 0, data.Length);
                }

                using (var decompressedStream = new MemoryStream(data))
                {
                    using (var resultStream = new GZipStream(decompressedStream, CompressionMode.Decompress))
                    {
                        lock (create)
                        {
                            resultStream.CopyTo(create);
                        }
                    }
                    pozition += tfile.BlockLength[i];
                    pozEvent += tfile.BlockLength[i];
                    AddProgressFile(tfile.Name, tfile.BlockLength[i], tfile.FileLength, pozEvent);
                }

            }
        }

        private void DecompressSmallFile(FileStream create, TFile title)
        {
            var data = new byte[title.FileLength];
            lock (Read)
            {
                Read.Position = title.PositionInTheStream;
                Read.Read(data,0,data.Length);
            }

            AddProgressFile(create.Name, title.FileLength);

            using (var decompressedStream = new MemoryStream(data))
            {
                using (var resultStream = new GZipStream(decompressedStream, CompressionMode.Decompress))
                {
                    lock (create)
                    {
                        resultStream.CopyTo(create);
                    }
                }

            }

        }

        private void CreateDir()
        {
            var timer = new Stopwatch();
            timer.Start();
            foreach (var t in Title.GetTitleDirectories())
            {
                var fullDir = Path.Combine(OutputDir, t.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(fullDir));
            }
            timer.Stop();
            var time = timer.ElapsedMilliseconds;

        }
        private bool IsDir()
        {
            string type;
            var buffer = new byte[3];
            Read.Read(buffer,0,buffer.Length);
            Read.Seek(-3, SeekOrigin.Current);
            try
            {
                type = Encoding.UTF8.GetString(buffer);
            }
            catch
            {
                type = "";
            }

            return type == "dir";
        }


    }
}

//ResultStream = resultStream;
//Title = new Title(resultStream);

//private Title Title;
//private DirectoryInfo MainDir;
//private Stream ResultStream;