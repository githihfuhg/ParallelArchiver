using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelArhive
{

    public class ParallelGz
    {
        private readonly int DegreeOfParallelism = Environment.ProcessorCount;
        private List<byte> Date = new List<byte>();

        public void CompressDirectory()
        {

        }
        public void DecompessDirectory()
        {

        }

        public void CompressFile(string Path)
        {
            //IEnumerable<string> paths = ...;
            //var processTasks = paths.Select(p => Task.Run(() => ProcessFile(p));
            //await Task.WhenAll(processTasks);
            var date = FileReadByte(Path);









        }

        public void DecompressFile()
        {

        }

        public byte[][] FileReadByte(string Path)
        {
           
            var fileInfo = new FileInfo(Path);
            long checkSize = fileInfo.Length / DegreeOfParallelism;
            bool honesty = checkSize % 2 == 0;

            var Bufer = Enumerable.Range(0, DegreeOfParallelism).Select(x => checkSize).ToList();

            if (!honesty)
            {
                Bufer[0] += 3;
            }

            using var stream = fileInfo.OpenRead();
            var parallelTask = Bufer.Select(x => Task.Factory.StartNew(() =>
                {

                    byte[] bytes = new byte[x];

                    //Offset += (int)checkSize;
                    stream.Read(bytes, 0, bytes.Length);

                    return bytes;

                }, TaskCreationOptions.LongRunning))
                .ToArray();

            Task.WaitAll(parallelTask);
            return parallelTask.Select(x => x.Result).ToArray();

        }

        private void AddDirectoryFilesToTar(TarArchive tarArchive, string sourceDirectory, bool recurse)
        {
            TarEntry tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
            tarArchive.WriteEntry(tarEntry, false);


            string[] filenames = Directory.GetFiles(sourceDirectory);
            foreach (string filename in filenames)
            {
                tarEntry = TarEntry.CreateEntryFromFile(filename);
                tarArchive.WriteEntry(tarEntry, true);
            }

            if (recurse)
            {
                string[] directories = Directory.GetDirectories(sourceDirectory);
                foreach (string directory in directories)
                    AddDirectoryFilesToTar(tarArchive, directory, recurse);
            }
        }

        private void CreateTar(string tgzFilename, string sourceDirectory)
        {
            Stream outStream = File.Create(tgzFilename);
            TarArchive tarArchive = TarArchive.CreateOutputTarArchive(outStream);
            tarArchive.RootPath = sourceDirectory.Replace('\\', '/');
            if (tarArchive.RootPath.EndsWith("/"))
                tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);
            AddDirectoryFilesToTar(tarArchive, sourceDirectory, true);
            tarArchive.Close();
        }

        private void CreateGZip(List<byte> data)
        {

        }



    }
}
