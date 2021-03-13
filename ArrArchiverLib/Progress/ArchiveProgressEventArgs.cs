namespace ArrArchiverLib.Progress
{
    public class ArchiveProgressEventArgs : System.EventArgs
    {
        public int AllProgress { get; set; }
        public string FileName { get; set; }
        public int CurrentFileProgress { get; set; }
        public long ElapsedMilliseconds { get; set; }
    }
}