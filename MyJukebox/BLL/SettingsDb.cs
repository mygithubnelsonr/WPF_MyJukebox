using MyJukebox.DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MyJukebox.BLL
{
    public static class SettingsDb
    {
        public static Dictionary<string, string> Settings;

        #region CTOR
        static SettingsDb()
        {
            Initalaze();
            Load();
        }
        #endregion

        private static void Initalaze()
        {
            var firstRun = GetSetting("FirstRunDate");

            if (String.IsNullOrEmpty(firstRun))
            {
                Settings["DatagridLastSelectedRow"] = "1";
                Settings["FirstRunDate"] = DateTime.UtcNow.ToString();
                Settings["LastRunDate"] = DateTime.UtcNow.ToString();
                Settings["FormHeight"] = "900";
                Settings["FormLeft"] = "0";
                Settings["FormTop"] = "0";
                Settings["FormWidth"] = "1200";
                Settings["ImagePath"] = "_Images";
                Settings["IsRandom"] = "false";
                Settings["LastAlbum"] = "";
                Settings["LastArtist"] = "";
                Settings["LastCatalog"] = "";
                Settings["LastGenre"] = "";
                Settings["LastPlaylist"] = "5";
                Settings["LastQuery"] = "";
                Settings["LastTab"] = "0";
                Settings["RecordEditorLocation"] = @"C:\Company\Apps\Multimedia\MyRecordEditor\MyRecordEditor.exe";
                Settings["RootImagePath"] = @"C:\\";
                Settings["Volume"] = "0.1";
                Settings["PlaceHolder"] = "enter search text here";
            }
        }

        public static void Load()
        {
            var context = new MyJukeboxEntities();
            var settings = context.tSettings
                            .Select(s => s).ToList();

            Settings = new Dictionary<string, string>();
            foreach (var s in settings)
                Settings.Add(s.Name, s.Value);

        }

        public static void Save()
        {
            var context = new MyJukeboxEntities();

            foreach (KeyValuePair<string, string> item in Settings)
            {
                var value = context.tSettings.SingleOrDefault(n => n.Name == item.Key);

                if (value == null)
                    DataGetSet.SetSetting(item.Key, (string)item.Value);
                else
                    value.Value = item.Value.ToString();
            }
            context.SaveChanges();
        }

        private static string GetSetting(string name)
        {
            var context = new MyJukeboxEntities();

            var result = context.tSettings
                                    .FirstOrDefault(s => s.Name == name);

            if (result == null || result.Value == "")
                return null;
            else
                return result.Value;
        }
    }
}
