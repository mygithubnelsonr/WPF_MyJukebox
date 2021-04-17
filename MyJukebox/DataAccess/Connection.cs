using System.Configuration;

namespace MyJukeboxWMPDapper.DataAccess
{
    public static class ConnectionTools
    {
        public static string GetConnectionString(string name = "MyJukeboxWMPDapper")
        {
            return ConfigurationManager.ConnectionStrings[name].ConnectionString;
        }

        public enum DataSourceEnum
        {
            Songs,
            Playlist,
            Query
        }

        public static DataSourceEnum Datasource { get; set; }
    }
}
