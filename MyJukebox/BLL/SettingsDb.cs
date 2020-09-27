using MyJukebox.DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace MyJukebox.BLL
{
    public static class SettingsDb
    {
        public static Dictionary<string, string> Settings;

        #region public properties
        // Query
        public static List<string> QueryList = new List<string>();
        // DatagridView
        public static SortedList<string, int> DatagridColums = new SortedList<string, int>();
        public static int DatagridLastSelectedRow { get; set; }

        // Form 6
        //public static int FormTop { get { return Convert.ToInt32(Settings["FormTop"]); } set { Settings["FormTop"] = value.ToString(); } }
        //public static int FormLeft { get { return Convert.ToInt32(Settings["FormLeft"]); } set { Settings["FormLeft"] = value.ToString(); } }
        //public static int FormHeight { get { return Convert.ToInt32(Settings["FormHeight"]); } set { Settings["FormHeight"] = value.ToString(); } }
        //public static int FormWidth { get { return Convert.ToInt32(Settings["FormWidth"]); } set { Settings["FormWidth"] = value.ToString(); } }
        //public static string FormState { get { return Convert.ToString(Settings["FormState"]); } set { Settings["FormState"] = value; } }
        //public static int FormSplitterLeft { get { return Convert.ToInt32(Settings["FormSplitterLeft"]); } set { Settings["FormSplitterLeft"] = value.ToString(); } }
        // Treeview 4
        //public static string LastGenre { get { return Convert.ToString(Settings["LastGenre"]); } set { Settings["LastGenre"] = value; } }
        //public static string LastCatalog { get { return Convert.ToString(Settings["LastCatalog"]); } set { Settings["LastCatalog"] = value; } }
        //public static string LastArtist { get { return Convert.ToString(Settings["LastArtist"]); } set { Settings["LastArtist"] = value; } }
        //public static string LastAlbum { get { return Convert.ToString(Settings["LastAlbum"]); } set { Settings["LastAlbum"] = value; } }
        // PlayList 1
        //public static int PlaylistCurrentID { get; set; }
        //public static string PlaylistCurrentName { get; set; }
        //public static int LastPlaylist { get { return Convert.ToInt32(Settings["LastPlaylist"]); } set { Settings["LastPlaylist"] = value.ToString(); } }
        // other settings 5
        //public static int LastTab { get { return Convert.ToInt32(Settings["LastTab"]); } set { Settings["LastTab"] = value.ToString(); } }
        //public static bool IsRandom { get { return Convert.ToBoolean(Settings["IsRandom"]); } set { Settings["IsRandom"] = value.ToString(); } }
        //public static string ImagePath { get { return Convert.ToString(Settings["ImagePath"]); } set { Settings["ImagePath"] = value; } }
        //public static string RootImagePath { get { return Convert.ToString(Settings["RootImagePath"]); } set { Settings["RootImagePath"] = value; } }
        //public static int Volume { get { return Convert.ToInt32(Settings["Volume"]); } set { Settings["Volume"] = value.ToString(); } }
        //public static string RecordEditorLocation { get { return Convert.ToString(Settings["RecordEditorLocation"]); } set { Settings["RecordEditorLocation"] = value; } }
        public readonly static string PlaceHolderText = $"  enter search text  ";
        #endregion public properties

        #region CTOR
        static SettingsDb()
        {
            Initalaze();
            Load();
        }
        #endregion

        public static int Formstate(string state)
        {
            int _state = 0;

            switch (state)
            {
                case "Normal":
                    _state = 0;
                    break;
                case "Minimized":
                    _state = 1;
                    break;
                case "Maximized":
                    _state = 2;
                    break;
            }
            return _state;
        }

        private static void Initalaze()
        {
            var firstRun = GetSetting("FirstRunDate");

            if (String.IsNullOrEmpty(firstRun))
            {
                Settings["DatagridLastSelectedRow"] = "1";
                Settings["FirstRunDate"] = DateTime.UtcNow.ToString();
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
                Settings["LastRunDate"] = DateTime.UtcNow.ToString();
                Settings["LastTab"] = "0";
                Settings["RecordEditorLocation"] = @"C:\Company\Apps\Multimedia\DbRecordEditor\DbRecordEditor.exe";
                Settings["RootImagePath"] = @"C:\\";
                Settings["Volume"] = "0.1";
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

            QueryList = DataGetSet.GetQueryList();
        }

        public static void Save()
        {
            var context = new MyJukeboxEntities();

            foreach (KeyValuePair<string, string> item in Settings)
            {
                Debug.Print(item.Key);

                var value = context.tSettings.SingleOrDefault(n => n.Name == item.Key);

                if (value == null)
                    DataGetSet.SetSetting(item.Key, (string)item.Value);
                else
                    value.Value = item.Value.ToString();
            }

            context.SaveChanges();

            var result = DataGetSet.TruncateTableQueries();
            foreach (string query in QueryList)
            {
                if (query != "")
                    context.tQueries.Add(new tQuery { Query = query });
            }

            context.SaveChanges();
        }

        private static string GetVisibility(MethodInfo accessor)
        {
            if (accessor.IsPublic)
                return "Public";
            else if (accessor.IsPrivate)
                return "Private";
            else if (accessor.IsFamily)
                return "Protected";
            else if (accessor.IsAssembly)
                return "Internal/Friend";
            else
                return "Protected Internal/Friend";
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
