namespace MyJukeboxWMPDapper.DataAccess
{
    public class AlbumModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int ID_Genre { get; set; }
        public int ID_Catalog { get; set; }
        public bool IsSampler { get; set; }
    }
}
