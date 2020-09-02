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
        private static Dictionary<string, object> _settings = new Dictionary<string, object>();

        #region public properties
        public readonly static string PlaceHolderText = $"  enter search text  ";
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
        public static int FormTop { get { return Convert.ToInt32(_settings["FormTop"]); } set { _settings["FormTop"] = value.ToString(); } }
        public static int FormLeft { get { return Convert.ToInt32(_settings["FormLeft"]); } set { _settings["FormLeft"] = value.ToString(); } }
        public static int FormHeight { get { return Convert.ToInt32(_settings["FormHeight"]); } set { _settings["FormHeight"] = value.ToString(); } }
        public static int FormWidth { get { return Convert.ToInt32(_settings["FormWidth"]); } set { _settings["FormWidth"] = value.ToString(); } }
        public static string FormState { get { return Convert.ToString(_settings["FormState"]); } set { _settings["FormState"] = value; } }
        public static int FormSplitterLeft { get { return Convert.ToInt32(_settings["FormSplitterLeft"]); } set { _settings["FormSplitterLeft"] = value.ToString(); } }
        // Treeview 4
        public static string LastGenre { get { return Convert.ToString(_settings["LastGenre"]); } set { _settings["LastGenre"] = value; } }
        public static string LastCatalog { get { return Convert.ToString(_settings["LastCatalog"]); } set { _settings["LastCatalog"] = value; } }
        public static string LastArtist { get { return Convert.ToString(_settings["LastArtist"]); } set { _settings["LastArtist"] = value; } }
        public static string LastAlbum { get { return Convert.ToString(_settings["LastAlbum"]); } set { _settings["LastAlbum"] = value; } }
        // PlayList 1
        public static int PlaylistCurrentID { get; set; }
        public static string PlaylistCurrentName { get; set; }
        public static int LastPlaylist { get { return Convert.ToInt32(_settings["LastPlaylist"]); } set { _settings["LastPlaylist"] = value.ToString(); } }
        // other settings 5
        public static int LastTab { get { return Convert.ToInt32(_settings["LastTab"]); } set { _settings["LastTab"] = value.ToString(); } }
        public static bool IsRandom { get { return Convert.ToBoolean(_settings["IsRandom"]); } set { _settings["IsRandom"] = value.ToString(); } }
        public static string ImagePath { get { return Convert.ToString(_settings["ImagePath"]); } set { _settings["ImagePath"] = value; } }
        public static string RootImagePath { get { return Convert.ToString(_settings["RootImagePath"]); } set { _settings["RootImagePath"] = value; } }
        public static int Volume { get { return Convert.ToInt32(_settings["Volume"]); } set { _settings["Volume"] = value.ToString(); } }

        #endregion public properties

        #region CTOR
        static SettingsDb()
        {
            var context = new MyJukeboxEntities();
            var settings = context.tSettings
                            .Select(s => s).ToList();

            _settings = new Dictionary<string, object>();
            foreach (var s in settings)
                _settings.Add(s.Name, s.Value);

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
                _settings["FirstRunDate"] = DateTime.UtcNow.ToString();
                _settings["LastTab"] = "0";
                _settings["ImagePath"] = "_Images";
                _settings["RootImagePath"] = @"C:\\";
                _settings["LastGenre"] = "";
                _settings["LastCatalog"] = "";
                _settings["LastAlbum"] = "";
                _settings["LastArtist"] = "";
                _settings["LastQuery"] = "";
                _settings["DatagridLastSelectedRow"] = "1";
            }
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
            LastPlaylist = Convert.ToInt16(GetSetting("LastPlaylist", "0"));
        }

        public static void Save()
        {
            var context = new MyJukeboxEntities();

            foreach (KeyValuePair<string, object> item in _settings)
            {
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

        public static void SetSetting(string name, object value)
        {
            _settings[name] = value;
        }

        internal static object GetSetting(string name, string init = "0")
        {
            if (!_settings.ContainsKey(name))
                SetSetting(name, init);

            return _settings[name];
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

    }
}
