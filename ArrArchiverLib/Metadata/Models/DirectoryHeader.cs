using System.Text;

namespace ArrArchiverLib.Metadata.Models
{
    public class DirectoryHeader
    {
        public int RelativePathLength { get; set; }
        public string RelativePath{ get; set; }
        public string FullPath{ get; set; } //Skipped

        public int SizeOf => sizeof(int) + RelativePathLength;

    }
}