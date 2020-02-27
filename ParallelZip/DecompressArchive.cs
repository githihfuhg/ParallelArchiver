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

    internal class DecompressArchive /*: ParallelArchiverEvents*/
    {
        private FileStream Read { get; set; }
        private string OutputDir { get; set; }
        private Title Title { get; set; }
        private ParallelArchiverEvents ParallelArchEvents { get; set; }
        //public event EventHandler<ICompressProgress> OnFileCompressProgress;
        internal DecompressArchive(ParallelArchiverEvents parallelArchiverEvents)
        {
            ParallelArchEvents = parallelArchiverEvents;
        }

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
                //var files = GetFiles();
                var type = Type(read);
                if (type == "dir")
                {
                    ParallelCreateFiles(false, fileExtension, fileName);
                }
                else if (type == "fil")
                {
                    ParallelCreateFiles();
                }
                else
                {
                    throw new Exception("The file is not an archive!!");
                }
            }

            GC.Collect();
            timer.Stop();
            var time = timer.ElapsedMilliseconds;

        }

        public string[] GetFiles(string path)
        {
            using (var read = new FileInfo(path).OpenRead())
            {
                var type = Type(read);
                if (type == "fil" || type == "dir")
                {
                    return new Title(read).GetTitleFiles().Select(tfile => tfile.Name).ToArray();
                }
                else
                {
                    throw new Exception("The file is not an archive!!");
                }
            }
        }

        private void ParallelCreateFiles(bool isOneFile = true, IEnumerable<string> fileName = null, IEnumerable<string> fileExtension = null)
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
                if (!isOneFile)
                {
                    CreateDir();
                }

                titles = allTitles;
            }
            ParallelArchEvents.Start(titles);
            var task = titles.Select(t => Task.Run(() =>
            {
                var fullDir = (isOneFile || fileName != null || fileExtension != null) ? Path.Combine(OutputDir, t.Name) : Path.Combine(OutputDir, t.FullName);
                using (var create = File.Open(fullDir, FileMode.OpenOrCreate, FileAccess.Write))
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
            ParallelArchEvents.Restart();

        }

        private void DecompressBigFile(FileStream create, TFile tfile)
        {
            //ProgressCounter progressCounter = new ProgressCounter(tfile);
            byte[] data;
            //var pozition = tfile.PositionInTheStream;
            for (long i = 0, pozEvent = 0, pozition = tfile.PositionInTheStream + 4; i < tfile.BlockCount; i++)
            {
                lock (Read)
                {
                    Read.Position = pozition;
                    data = new byte[tfile.BlockLength[i]];
                    Read.Read(data, 0, data.Length);
                }
                DecompreesBlock(create, data, tfile.TypeСompression);
                pozition += tfile.BlockLength[i] + 4;
                pozEvent += tfile.BlockLength[i];
                ParallelArchEvents.AddProgressFile(tfile.Name, tfile.BlockLength[i], tfile.FileLength, pozEvent);
            }
        }
        private void DecompressSmallFile(FileStream create, TFile tfile)
        {
            var data = new byte[tfile.FileLength];
            lock (Read)
            {
                Read.Position = tfile.PositionInTheStream;
                Read.Read(data, 0, data.Length);
                ParallelArchEvents.AddProgressFile(tfile.Name, tfile.FileLength);
            }
            DecompreesBlock(create, data, tfile.TypeСompression);

        }

        private void DecompreesBlock(FileStream create, byte[] data, string typeCompression)
        {
            using (var decompressedStream = new MemoryStream(data))
            {
                if (typeCompression == "gz")
                {
                    using (var resultStream = new GZipStream(decompressedStream, CompressionMode.Decompress))
                    {
                        lock (create)
                        {
                            resultStream.CopyTo(create);
                        }
                    }
                }
                else if (typeCompression == "br")
                {
                    using (var resultStream = new BrotliStream(decompressedStream, CompressionMode.Decompress))
                    {
                        lock (create)
                        {
                            resultStream.CopyTo(create);
                        }
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
        public string Type(FileStream read)
        {
            string type;
            var buffer = new byte[3];
            read.Position = 0;
            read.Read(buffer, 0, buffer.Length);
            try
            {
                type = Encoding.UTF8.GetString(buffer);
            }
            catch
            {
                type = "";
            }
            read.Position = 0;
            return type;
        }



    }

}

//ResultStream = resultStream;
//Title = new Title(resultStream);

//private Title Title;
//private DirectoryInfo MainDir;
//private Stream ResultStream;
