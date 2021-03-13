namespace ArrArchiverLib.Metadata.Models
{
    public class ChunkHeader
    {
        public int SerialNumber { get; set; }
        public int Size { get; set; }

        public int SizeOf => sizeof(int) + sizeof(int); //Skipped
    }
}