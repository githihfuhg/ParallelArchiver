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
                if (Type() == "dir")
                {
                    ParallelCreateFiles(false, fileExtension, fileName);
                }
                else if(Type() == "fil")
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

        private void ParallelCreateFiles(bool isOneFiele = true, IEnumerable<string> fileName = null, IEnumerable<string> fileExtension = null)
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
                if (!isOneFiele)
                {
                    CreateDir();
                }

                titles = allTitles;
            }
            Start(titles);
            var task = titles.Select(t => Task.Run(() =>
            {
                var fullDir = (isOneFiele || fileName!= null || fileExtension!=null) ? Path.Combine(OutputDir, t.Name) : Path.Combine(OutputDir, t.FullName);
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
            //var pozition = tfile.PositionInTheStream;
            for (long i = 0, pozEvent = 0,pozition = tfile.PositionInTheStream; i < tfile.BlockCount; i++)
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

            AddProgressFile(title.Name, title.FileLength);

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
        private string Type()
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


            return type;
        }
        //private bool IsText(string Name)
        //{
        //    string e = Path.GetExtension(Name);

        //}
        private string[] Extension =
        {
            ".doc",".docx",".1st",".602",".abw",".act",".adoc",".aim",".ans",
            ".asc",".asc",".ase",".awp",".aww",".bad",".bbs",".bdp",".bdr",
            ".bean",".bib",".bib",".bibtex",".bml",".bna",".boc",".brx",".btd",
            ".bzabw",".calca",".charset",".chord",".cnm",".cod",".crwl",".cws",
            ".cyi",".dgs",".diz",".dne",".doc",".doc",".docm",".docx",".dox",
            ".dsc",".dvi",".dwd",".dxb",".dxp",".eio",".eit",".emf",".eml",".emlx",
            ".epp",".err",".err",".etf",".etx",".euc",".fbl",".fcf",".fdf",".fdr",
            ".fds",".fdt",".fdx",".fdxt",".fft",".fgs",".flr",".fodt",".fountain",
            ".fpt",".frt",".fwdn",".gmd",".gpd",".gpn",".gsd",".gthr",".gv",".hbk",
            ".hht",".hs",".hwp",".hwp",".hz",".idx",".iil",".ipf",".ipspot",".jarvis",
            ".jis",".jnp",".joe",".jp1",".jrtf",".jtd",".kes",".klg",".klg",".knt",
            ".kon",".kwd",".latex",".lbt",".lis",".lnt",".log",".lp2",".lst",".lst",
            ".ltr",".ltx",".lue",".luf",".lwp",".lxfml",".lyt",".lyx",".mbox",".mcw",
            ".txt",".mell",".mellel",".mnt",".msg",".mw",".mwd",".mwp",".nb",".ndoc",
            ".nfo",".ngloss",".njx",".note",".notes",".now",".nwctxt",".nwm",".nwp",
            ".ocr",".odif",".odm",".odo",".odt",".ofl",".opeico",".openbsd",".ort",
            ".ott",".p7s",".pages",".pfx",".plantuml",".pmo",".prt",".prt",".psw",
            ".pu",".pvm",".pwd",".pwi",".qdl",".qpf",".rad",".readme",".rft",".ris",
            ".rpt",".rst",".rtd",".rtf",".rtfd",".rtx",".run",".rvf",".rzk",".rzn",
            ".saf",".safetext",".scc",".scm",".scriv",".scrivx",".sct",".scw",".sdw",
            ".session",".sgm",".sig",".sla",".gz",".smf",".sms",".ssa",".story",
            ".strings",".sty",".sxw",".tab",".tab",".tdf",".tdf",".template",
            ".tex",".text",".thp",".tlb",".tm",".tmd",".tmdx",".tmv",".tmvx",
            ".tpc",".trelby",".tvj",".txt",".u3i",".unauth",".unx",".uof",".uot",
            ".upd",".utf8",".utxt",".vct",".vnt",".vw",".wbk",".webdoc",".net",
            ".wn",".wp",".wp4",".wp5",".wp6",".wp7",".wpa",".wpd",".wpd",".wpd",
            ".wpl",".wps",".wps",".wpt",".wpt",".wpw",".wri",".wsd",".wtt",".wtx",
            ".xbdoc",".xbplate",".xdl",".xdl",".xwp",".xwp",".xwp",".xy",".xy3",
            ".xyp",".xyw",".zabw",".zrtf",".cpp",".c",".cs",".py",".css",".html",
            ".xml",".json",".text",
       };
       

    }
}

//ResultStream = resultStream;
//Title = new Title(resultStream);

//private Title Title;
//private DirectoryInfo MainDir;
//private Stream ResultStream;