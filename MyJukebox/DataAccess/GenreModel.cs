namespace MyJukeboxWMPDapper.DataAccess
{
    public class GenreModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string LastCatalog { get; set; } = "Alle";
        public int LastCatalogID { get; set; } = 0;
    }
}
