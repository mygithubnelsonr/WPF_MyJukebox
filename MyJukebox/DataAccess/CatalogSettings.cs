namespace MyJukeboxWMPDapper.DataAccess
{
    public class CatalogSetting
    {
        public int ID { get; set; }
        public string Catalog { get; set; }
        public int ID_Catalog { get; set; }
        public string Album { get; set; }
        public int ID_Album { get; set; }
        public int Row { get; set; }
    }
}
