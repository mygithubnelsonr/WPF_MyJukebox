namespace MyJukeboxWMPDapper.DataAccess
{
    public class SettingsModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Editable { get; set; }
    }
}
