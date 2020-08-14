using System;

namespace MyJukebox.Common
{
    public class MP3Record
    {
        public int Genre { get; set; }
        public int Catalog { get; set; }
        public int Media { get; set; }
        public string Album { get; set; }
        public string Artist { get; set; }
        public string Titel { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }
        public int FileSize { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime FileDate { get; set; }
        public string MD5 { get; set; }
        public int Played { get; set; }
        public int Rating { get; set; }
        public string Beat { get; set; }
        public string Comment { get; set; }
        public bool IsSample { get; set; }
        public bool Error { get; set; }
        public bool Hide { get; set; }
    }
}
