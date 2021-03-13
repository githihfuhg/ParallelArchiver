using System.Text;
using ArrArchiverLib.Resources;

namespace ArrArchiverLib.Metadata.Models
{
    public class ArchiveInfo
    {
        public string Header { get; set; } = ArchiveResource.ArchiveHeader;
        public bool IsEncrypted { get; set; }
        public int NumberOfDirectories { get; set; }
        public int DirectoriesBlockPosition { get; set; }
        public int NumberOfFiles { get; set; }
        public long FilesBlockPosition { get; set; }
        public int SizeOf => HeaderLenght + sizeof(bool) + sizeof(int) + sizeof(int) + sizeof(int) + sizeof(long);  //Skipped
        public int HeaderLenght => Encoding.UTF8.GetBytes(Header).Length; //Skipped
    }
}