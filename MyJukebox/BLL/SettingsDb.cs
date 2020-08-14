using MyJukebox.DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace MyJukebox.BLL
{
    public class SettingsDb
    {
        private static Dictionary<string, object> Settings = new Dictionary<string, object>();
        //public readonly static string PlaceHolderText = $"  < Input SQL like Album='V8-A-1' >";
        public readonly static string PlaceHolderText = $"  enter search text  ";

        #region public properties

        // Query
        public static List<string> QueryList = new List<string>();
        // DatagridView
        public static SortedList<string, int> DatagridColums = new SortedList<string, int>();
        public static int DatagridLastSelectedRow { get; set; }
        // Filescanner
        public static int FilescannerHeight { get; set; }
        public static int FilescannerLeft { get; set; }
        public static int FilescannerTop { get; set; }
        public static int FilescannerWidth { get; set; }
        public static string ForbChars { get; set; }
        // Form 6
        public static int FormTop { get { return Convert.ToInt32(Settings["FormTop"]); } set { Settings["FormTop"] = value.ToString(); } }
        public static int FormLeft { get { return Convert.ToInt32(Settings["FormLeft"]); } set { Settings["FormLeft"] = value.ToString(); } }
        public static int FormHeight { get { return Convert.ToInt32(Settings["FormHeight"]); } set { Settings["FormHeight"] = value.ToString(); } }
        public static int FormWidth { get { return Convert.ToInt32(Settings["FormWidth"]); } set { Settings["FormWidth"] = value.ToString(); } }
        public static string FormState { get { return Convert.ToString(Settings["FormState"]); } set { Settings["FormState"] = value; } }
        public static int FormSplitterLeft { get { return Convert.ToInt32(Settings["FormSplitterLeft"]); } set { Settings["FormSplitterLeft"] = value.ToString(); } }
        // Treeview 4
        public static string LastGenre { get { return Convert.ToString(Settings["LastGenre"]); } set { Settings["LastGenre"] = value; } }
        public static string LastCatalog { get { return Convert.ToString(Settings["LastCatalog"]); } set { Settings["LastCatalog"] = value; } }
        public static string LastArtist { get { return Convert.ToString(Settings["LastArtist"]); } set { Settings["LastArtist"] = value; } }
        public static string LastAlbum { get { return Convert.ToString(Settings["LastAlbum"]); } set { Settings["LastAlbum"] = value; } }
        // PlayList 1
        public static int PlaylistCurrentID { get; set; }
        public static string PlaylistCurrentName { get; set; }
        public static int LastPlaylist { get { return Convert.ToInt32(Settings["LastPlaylist"]); } set { Settings["LastPlaylist"] = value.ToString(); } }
        // other settings 5
        public static int LastTab { get { return Convert.ToInt32(Settings["LastTab"]); } set { Settings["LastTab"] = value.ToString(); } }
        public static bool IsRandom { get { return Convert.ToBoolean(Settings["IsRandom"]); } set { Settings["IsRandom"] = value.ToString(); } }
        public static string ImagePath { get { return Convert.ToString(Settings["ImagePath"]); } set { Settings["ImagePath"] = value; } }
        public static string RootImagePath { get { return Convert.ToString(Settings["RootImagePath"]); } set { Settings["RootImagePath"] = value; } }
        public static int Volume { get { return Convert.ToInt32(Settings["Volume"]); } set { Settings["Volume"] = value.ToString(); } }

        #endregion public properties

        #region CTOR
        static SettingsDb()
        {
            var context = new MyJukeboxEntities();
            var settings = context.tSettings
                            .Select(s => s).ToList();

            Settings = new Dictionary<string, object>();
            foreach (var s in settings)
                Settings.Add(s.Name, s.Value);

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

        public static void Initalaze()
        {
            var firstRun = GetSetting("FirstRunDate");

            if (String.IsNullOrEmpty(firstRun.ToString()))
            {
                Settings["FirstRunDate"] = DateTime.UtcNow.ToString();
                Settings["LastTab"] = "0";
                Settings["ImagePath"] = "_Images";
                Settings["RootImagePath"] = @"C:\\";
                Settings["LastGenre"] = "";
                Settings["LastCatalog"] = "";
                Settings["LastAlbum"] = "";
                Settings["LastArtist"] = "";
                Settings["LastQuery"] = "";
                Settings["DatagridLastSelectedRow"] = "1";
            }
        }

        public static void Test()
        {
            //foreach (KeyValuePair<string, string> item in Settings)
            //    Debug.Print($"{item.Key}, {item.Value}");

            // https://docs.microsoft.com/en-us/dotnet/api/system.type.getproperties?f1url=https%3A%2F%2Fmsdn.microsoft.com%2Fquery%2Fdev16.query%3FappId%3DDev16IDEF1%26l%3DEN-US%26k%3Dk(System.Type.GetProperties);k(TargetFrameworkMoniker-.NETFramework,Version%3Dv4.7.2);k(DevLang-csharp)%26rd%3Dtrue&view=netframework-4.8
            // https://www.codeproject.com/Articles/667438/How-to-iterate-through-all-properties-of-a-class

            Type t = typeof(SettingsDb);

            //PropertyInfo[] propInfos = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo[] propInfos = t.GetProperties();

            Debug.Print("The number of public properties: {0}.\n", propInfos.Length);

            // Display the public properties.
            DisplayPropertyInfo(propInfos);

            //// Get the nonpublic properties.
            //PropertyInfo[] propInfos1 = t.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
            //Console.WriteLine("The number of non-public properties: {0}.\n",
            //                  propInfos1.Length);
            //// Display all the nonpublic properties.
            //DisplayPropertyInfo(propInfos);
        }

        private static void DisplayPropertyInfo(PropertyInfo[] propInfos)
        {
            // Display information for all properties.
            foreach (var propInfo in propInfos)
            {
                bool readable = propInfo.CanRead;
                bool writable = propInfo.CanWrite;

                Console.WriteLine("   Property name: {0}", propInfo.Name);
                Console.WriteLine("   Property type: {0}", propInfo.PropertyType);
                Console.WriteLine("   Read-Write:    {0}", readable & writable);
                if (readable)
                {
                    MethodInfo getAccessor = propInfo.GetMethod;
                    Console.WriteLine("   Visibility:    {0}",
                                      GetVisibility(getAccessor));
                }
                if (writable)
                {
                    MethodInfo setAccessor = propInfo.SetMethod;
                    Console.WriteLine("   Visibility:    {0}",
                                      GetVisibility(setAccessor));
                }
                Console.WriteLine();
            }
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

        public static void Load()
        {
            LastTab = Convert.ToInt16(GetSetting("LastTab", "0"));
            DatagridLastSelectedRow = Convert.ToInt16(GetSetting("DatagridLastSelectedRow", "1"));
            IsRandom = Convert.ToBoolean(GetSetting("IsRandom", "False"));
            Volume = Convert.ToInt32(GetSetting("Volume", "0"));
            LastAlbum = Convert.ToString(GetSetting("LastAlbum", "Alle"));
            LastCatalog = Convert.ToString(GetSetting("LastCatalog", "Alle"));
            LastGenre = Convert.ToString(GetSetting("LastGenre", "Alle"));
            LastArtist = Convert.ToString(GetSetting("LastArtist", "Alle"));
            FormTop = Convert.ToInt16(GetSetting("FormTop", "100"));
            FormLeft = Convert.ToInt16(GetSetting("FormLeft", "100"));
            FormWidth = Convert.ToInt16(GetSetting("FormWidth", "836"));
            FormHeight = Convert.ToInt16(GetSetting("FormHeight", "580"));
            FormSplitterLeft = Convert.ToInt16(GetSetting("FormSplitterLeft", "200"));
            FormState = Convert.ToString(GetSetting("FormState", "Normal"));
            RootImagePath = Convert.ToString(GetSetting("RootImagePath", "C:\\"));
            QueryList = DataGetSet.GetQueryList();
        }

        public static void Save()
        {
            var context = new MyJukeboxEntities();

            foreach (KeyValuePair<string, object> item in Settings)
            {
                var update = context.tSettings.SingleOrDefault(n => n.Name == item.Key);
                update.Value = item.Value.ToString();
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

        public static void SetSetting(string name, object value)
        {
            Settings[name] = value;
        }

        internal static object GetSetting(string name, string init = "0")
        {
            var setting = Settings[name];
            if (setting != null)
                return setting;
            else
                SetSetting(name, init);

            return init;
        }
    }
}
