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
                //var files = GetFiles();
                if (Type() == "dir")
                {
                    ParallelCreateFiles(false, fileExtension, fileName);
                }
                else if (Type() == "fil")
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

        public string[] GetFiles()
        {
            return Title.GetTitleFiles().Select(tfile => tfile.Name).ToArray();
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
            Start(titles);
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
            Restart();

        }

        private void DecompressBigFile(FileStream create, TFile tfile)
        {
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
                AddProgressFile(tfile.Name, tfile.BlockLength[i], tfile.FileLength, pozEvent);

            }
        }
        private void DecompressSmallFile(FileStream create, TFile tfile)
        {
            var data = new byte[tfile.FileLength];
            lock (Read)
            {
                Read.Position = tfile.PositionInTheStream;
                Read.Read(data, 0, data.Length);
            }
            DecompreesBlock(create, data, tfile.TypeСompression);
            AddProgressFile(tfile.Name, tfile.FileLength);
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
        private string Type()
        {
            string type;
            var buffer = new byte[3];
            Read.Position = 0;
            Read.Read(buffer, 0, buffer.Length);
            try
            {
                type = Encoding.UTF8.GetString(buffer);
            }
            catch
            {
                type = "";
            }
            Read.Position = 0;
            return type;
        }



    }
}

//ResultStream = resultStream;
//Title = new Title(resultStream);

//private Title Title;
//private DirectoryInfo MainDir;
//private Stream ResultStream;