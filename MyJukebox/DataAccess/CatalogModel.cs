namespace MyJukeboxWMPDapper.DataAccess
{
    public class CatalogModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string LastAlbum { get; set; }
        public int LastAlbumID { get; set; }
        public string LastArtist { get; set; }
    }
}
