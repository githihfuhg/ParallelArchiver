using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace test
{
    internal class TarArh
    {
        private readonly DateTime dateTime1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private const long timeConversionFactor = 10000000L;

        private string Name { get; set; }

        private long FileLength { get; set; }

        private int mode;
        private int userId;
        private int groupId;
        private long size;
        private /*DateTime*/ long modTime;

        private int checksum;

        //private bool isChecksumValid;
        private byte typeFlag;
        private string linkName;
        private string magic;
        private string version;
        private string userName;
        private string groupName;
        private int devMajor;
        private int devMinor;

        private byte[] GetName
        {
            get
            {
                var name = new byte[100];
                Encoding.UTF8.GetBytes(Name).CopyTo(name, 0);
                return name;
            }
        }

        private byte[] Mode => Name.EndsWith("/", StringComparison.Ordinal)
            ? Encoding.UTF8.GetBytes($"{long.Parse(Convert.ToString(16895, 8)):D7}\0")
            : Encoding.UTF8.GetBytes($"{long.Parse(Convert.ToString(33279, 8)):D7}\0");

        private byte[] UserId => Encoding.UTF8.GetBytes("{0:D7}\0");
        private byte[] GroupId => Encoding.UTF8.GetBytes("{0:D7}\0");
        private byte[] Size => Encoding.UTF8.GetBytes($"{long.Parse(Convert.ToString(FileLength, 8)):D11}\0");
        private byte[] ModTime
        {
            get
            {
                var date = (DateTime.UtcNow.Ticks - dateTime1970.Ticks) / timeConversionFactor;
                return Encoding.UTF8.GetBytes($"{long.Parse(Convert.ToString(date, 8))}\0");
            }
        }
        private byte[] Checksum { get; set; } = new byte[8].Select(x => (byte)32).ToArray();
        private byte TypeFlag => Name.EndsWith("/", StringComparison.Ordinal) ? (byte)'5' : (byte)'0';
        private byte[] LinkName => new byte[100];
        private byte[] Magic => Encoding.UTF8.GetBytes("ustar\0");
        private byte[] Version => Encoding.UTF8.GetBytes("00");
        private byte[] UserName => new byte[32];
        private byte[] GroupName => new byte[32];
        private byte[] DevMajor => new byte[8];
        private byte[] DevMinor => new byte[8];
        private byte[] TheEnd => new byte[167];

        public void check(string Input)
        {

            using (var stream = File.OpenRead(Input))
            {
                var timer = new Stopwatch();
                timer.Start();
                while (stream.Position < stream.Length)
                {
                    //var summbuf = new byte[512];
                    //stream.Read(summbuf, 0, 512);
                    //var summa = summbuf.Select(x => (int)x).ToArray().Sum();
                    var buffer = new byte[155];
                    stream.Read(buffer, 0, 100);
                    Name = Encoding.UTF8.GetString(buffer, 0, 100).Trim('\0');




                    if (!string.IsNullOrWhiteSpace(Name))
                    {

                        stream.Read(buffer, 0, 8);
                        var Mode = Encoding.UTF8.GetString(buffer, 0, 8);
                        mode = Convert.ToInt32(Encoding.UTF8.GetString(buffer, 0, 8).Trim('\0').Trim(), 8);

                        stream.Read(buffer, 0, 8);
                        var UserId = Encoding.UTF8.GetString(buffer, 0, 8);
                        userId = Convert.ToInt32(Encoding.UTF8.GetString(buffer, 0, 8).Trim('\0').Trim(), 8);

                        stream.Read(buffer, 0, 8);
                        var GroupId = Encoding.UTF8.GetString(buffer, 0, 8);
                        groupId = (Convert.ToInt32(Encoding.UTF8.GetString(buffer, 0, 8).Trim('\0').Trim(), 8));

                        stream.Read(buffer, 0, 12);
                        var Size = Encoding.UTF8.GetString(buffer, 0, 12);
                        size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);

                        stream.Read(buffer, 0, 12);
                        var ModeTime = Encoding.UTF8.GetString(buffer, 0, 12);
                        modTime = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);
                        var times = new DateTime(dateTime1970.Ticks + modTime * timeConversionFactor);

                        stream.Read(buffer, 0, 8);
                        var CheckSum = Encoding.UTF8.GetString(buffer, 0, 8);
                        checksum = Convert.ToInt32(Encoding.UTF8.GetString(buffer, 0, 8).Trim().Trim('\0'), 8);

                        stream.Read(buffer, 0, 1);
                        var TypeFlag = Encoding.UTF8.GetString(buffer, 0, 1);
                        typeFlag = buffer[0];


                        stream.Read(buffer, 0, 100);
                        var LinkName = Encoding.UTF8.GetString(buffer, 0, 100);
                        linkName = Encoding.UTF8.GetString(buffer, 0, 100).Trim('\0');

                        stream.Read(buffer, 0, 6);
                        var Magic = Encoding.UTF8.GetString(buffer, 0, 6);
                        magic = Encoding.UTF8.GetString(buffer, 0, 6).Trim('\0');

                        stream.Read(buffer, 0, 2);
                        var Version = Encoding.UTF8.GetString(buffer, 0, 2);
                        version = Encoding.UTF8.GetString(buffer, 0, 2).Trim('\0');

                        stream.Read(buffer, 0, 32);
                        var UserName = Encoding.UTF8.GetString(buffer, 0, 32);
                        userName = Encoding.UTF8.GetString(buffer, 0, 32).Trim('\0');

                        stream.Read(buffer, 0, 32);
                        var GroupName = Encoding.UTF8.GetString(buffer, 0, 32);
                        groupName = Encoding.UTF8.GetString(buffer, 0, 32).Trim('\0');

                        stream.Read(buffer, 0, 8);
                        devMajor = BitConverter.ToInt32(buffer, 0);

                        stream.Read(buffer, 0, 8);
                        devMinor = BitConverter.ToInt32(buffer, 0);

                        stream.Read(buffer, 0, 155);
                        var Prefix = Encoding.UTF8.GetString(buffer);
                        var prefix = Encoding.UTF8.GetString(buffer).Trim('\0');

                        stream.Read(buffer, 0, 12);
                        var Mfill = Encoding.UTF8.GetString(buffer, 0, 12);
                        var mfill = Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0');
                        stream.Seek(size, SeekOrigin.Current);

                    }
                    var offset = 512 - (stream.Position % 512);
                    if (offset != 512)
                        stream.Seek(offset, SeekOrigin.Current);
                }
                timer.Stop();
                var res = timer.ElapsedMilliseconds;
            }
        }


        internal byte[] AddBlock(string input, long lenght = 0)
        {
            FileLength = lenght;
            Name = input;
            var title = new byte[512];
            GetName.CopyTo(title, 0);
            Mode.CopyTo(title, 100);
            UserId.CopyTo(title, 108);
            GroupId.CopyTo(title, 116);
            Size.CopyTo(title, 124);
            ModTime.CopyTo(title, 136);
            Checksum.CopyTo(title, 148);
            title[156] = TypeFlag;
            LinkName.CopyTo(title, 157);
            Magic.CopyTo(title, 257);
            Version.CopyTo(title, 263);
            UserName.CopyTo(title, 265);
            GroupName.CopyTo(title, 297);
            DevMajor.CopyTo(title, 329);
            DevMinor.CopyTo(title, 337);
            TheEnd.CopyTo(title, 345);

            Checksum = Encoding.UTF8.GetBytes($"{Convert.ToString(title.Select(x => (int)x).Sum(), 8):D7}\0");
            for (int i = 148; i < Checksum.Length + 148; i++)
            {
                title[i] = Checksum[i - 148];
            }
            return title;

        }
        public static void ExtractTar(string filename, string outputDir)
        {
            using (var stream = File.OpenRead(filename))
            {
                while (stream.Position < stream.Length)
                {
                    var buffer = new byte[100];
                    stream.Read(buffer, 0, 100);
                    var name = Encoding.UTF8.GetString(buffer).Trim('\0');
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        stream.Seek(24, SeekOrigin.Current);
                        stream.Read(buffer, 0, 12);
                        var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);
                        stream.Seek(376L, SeekOrigin.Current);
                        //stream.Seek(size, SeekOrigin.Current);
                        var output = Path.Combine(outputDir, name);
                        Directory.CreateDirectory(Path.GetDirectoryName(output));
                        if (!name.EndsWith("/") /*&&name.EndsWith("\\")*/)
                        {
                            using (var str = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                var buf = new byte[size];
                                stream.Read(buf, 0, buf.Length);
                                str.Write(buf, 0, buf.Length);
                            }
                        }

                        var offset = 512 - (stream.Position % 512);
                        if (offset != 512)
                            stream.Seek(offset, SeekOrigin.Current);
                    }
                }
            }

        }

    }



}






//stream.Read(buffer, 0, 100);
//var name = Encoding.UTF8.GetString(buffer).Trim('\0');
//if (string.IsNullOrWhiteSpace(name))
//    break;
//stream.Seek(24, SeekOrigin.Current);
//stream.Read(buffer, 0, 12);
//var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);

//stream.Seek(376L, SeekOrigin.Current);

//var output = Path.Combine(outputDir, name);
//if (!Directory.Exists(Path.GetDirectoryName(output)))
//    Directory.CreateDirectory(Path.GetDirectoryName(output));
//if (!name.EndsWith("/"))
//{
//    using (var str = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write))
//    {
//        var buf = new byte[size];
//        stream.Read(buf, 0, buf.Length);
//        //if (name != "././@LongLink")
//        //{
//        str.Write(buf, 0, buf.Length);
//        //}
//    }


//}

//var pos = stream.Position;

//var offset = 512 - (pos % 512);
//if (offset == 512)
//    offset = 0;

//stream.Seek(offset, SeekOrigin.Current);


//foreach (var path in direct)
//{
//    var files = directoryInfo.GetFiles();
//    var buffer = new byte[100];
//    var buferName = Encoding.Default.GetBytes($"{path.Name}/");
//    for (int i = 0; i < buferName.Length; i++)
//    {
//        buffer[i] = buferName[i];
//    }
//    resultStream.Write(buffer, 0, buffer.Length);
//    resultStream.Seek(24, SeekOrigin.Current);
//    //var size = Convert.ToByte(path.Length);
//    resultStream.Write(BitConverter.GetBytes(path.Name.Length), 0, 8);
//    resultStream.Seek(376L, SeekOrigin.Current);

//    Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim();
//    var d = Encoding.UTF8.GetBytes($"{path.Name.Length}");

//}

//using (var stream = File.OpenRead(filename))
//{
//    while (stream.Position < stream.Length)
//    {
//        var buffer = new byte[100];
//        stream.Read(buffer, 0, 100);
//        var name = Encoding.UTF8.GetString(buffer).Trim('\0');
//        if (!string.IsNullOrWhiteSpace(name))
//        {
//            stream.Seek(24, SeekOrigin.Current);
//            stream.Read(buffer, 0, 12);
//            var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);
//            stream.Seek(376L, SeekOrigin.Current);
//            //stream.Seek(size, SeekOrigin.Current);
//            var output = Path.Combine(outputDir, name);
//            Directory.CreateDirectory(Path.GetDirectoryName(output));
//            if (!name.EndsWith("/"))
//            {
//                using (var str = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write))
//                {

//                    var buf = new byte[size];
//                    stream.Read(buf, 0, buf.Length);
//                    str.Write(buf, 0, buf.Length);
//                }
//            }

//            var offset = 512 - (stream.Position % 512);
//            if (offset != 512)
//                stream.Seek(offset, SeekOrigin.Current);
//        }
//    }
//}

//public void CompressDirectorytgz(string inputDirectory, string result)
//{
//    var timer = new Stopwatch();
//    timer.Start();
//    var directoryInfo = new DirectoryInfo(inputDirectory);
//    var direct = directoryInfo.EnumerateDirectories("*", SearchOption.AllDirectories).ToArray();
//    //var pathFile = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).ToArray();
//    using FileStream resultStream = File.Create(result);


//    foreach (var dir in direct)
//    {
//        var files = directoryInfo.GetFiles();
//        var buffer = new byte[100];
//        var buferName =
//            Encoding.UTF8.GetBytes($"{directoryInfo.Name}/{dir.FullName.Replace(directoryInfo.FullName, "")}/");
//        resultStream.Write(buferName);
//        resultStream.Seek((100 - buferName.Length) + 24, SeekOrigin.Current);
//        //"00000003524\0"
//        resultStream.Write(Encoding.UTF8.GetBytes($"00000000000\0"));
//        resultStream.Seek(376L, SeekOrigin.Current);
//        var pathFiles = dir.GetFiles();
//        var offset = 512 - (resultStream.Position % 512);
//        if (offset != 512)
//            resultStream.Seek(offset, SeekOrigin.Current);

//        foreach (var path in dir.GetFiles())
//        {
//            var fileName = Encoding.UTF8.GetBytes(Path.Combine(directoryInfo.Name,
//                path.FullName.Replace(directoryInfo.FullName, "")));
//            resultStream.Write(fileName);
//            resultStream.Seek((100 - fileName.Length) + 24, SeekOrigin.Current);
//            var asfsaf = $"{long.Parse(Convert.ToString(path.Length, 8)):D11}\0";
//            var size = Encoding.UTF8.GetBytes($"{long.Parse(Convert.ToString(path.Length, 8)):D11}\0");
//            resultStream.Write(size);
//            resultStream.Seek(376L, SeekOrigin.Current);
//            using (var readFile = path.OpenRead())
//            {
//                readFile.CopyTo(resultStream);
//            }

//            var offsetes = 512 - (resultStream.Position % 512);
//            if (offsetes != 512)
//                resultStream.Seek(offsetes, SeekOrigin.Current);
//        }

//    }

//    resultStream.Dispose();
//    timer.Stop();
//    var time = timer.ElapsedMilliseconds;
//}
