using MyJukeboxWMPDapper.Common;
using MyJukeboxWMPDapper.DataAccess;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MyJukeboxWMPDapper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    // ToDo Implement WMP

    public partial class MainWindow : Window
    {
        #region Fields
        AxWMPLib.AxWindowsMediaPlayer player = new AxWMPLib.AxWindowsMediaPlayer();

        //private ObservableCollection<string> _genres;
        //private ObservableCollection<string> _catalogs;
        //private ObservableCollection<string> _artists;
        //private ObservableCollection<string> _albums;
        //private ObservableCollection<Query> _queries;

        private ObservableCollection<string> _queries;
        private ObservableCollection<PlaylistModel> _playlists;
        private List<string> _artistImageList;

        private int _lastID = -1;
        private int _lastRow = -1;
        private int _lastTab = -1;
        private int _lastGenreID = -1;
        private int _lastCatalogID = -1;
        private int _lastAlbumID = -1;
        private int _lastArtistID = -1;
        private int _lastPlaylistID = -1;
        private string _lastPlaylist = "";
        private string _lastQuery = "";
        private string _artist = "";
        private string _lastArtist = "";
        private bool _dataLoaded = false;
        private bool _isLoaded = false;
        private bool _userIsDraggingSlider;
        private bool _mediaPlayerIsPlaying = false;

        private RandomH random = new RandomH();
        private DispatcherTimer timerDuration;
        private DispatcherTimer timerFlipImage;
        private DispatcherTimer timerEditor;

        #endregion

        public static bool processEnded = false;

        #region CTOR
        public MainWindow()
        {
            InitializeComponent();

            InitMediaPlayer();

            #region Configure Timers
            timerDuration = new DispatcherTimer();
            timerDuration.Interval = TimeSpan.FromSeconds(1);
            timerDuration.Tick += timerDuration_Tick;
            timerDuration.IsEnabled = false;

            timerFlipImage = new DispatcherTimer();
            timerFlipImage.Interval = TimeSpan.FromSeconds(10);
            timerFlipImage.Tick += timerFlipImage_Tick;
            timerFlipImage.IsEnabled = true;
            timerFlipImage.Stop();

            timerEditor = new DispatcherTimer();
            timerEditor.Interval = TimeSpan.FromSeconds(0.1);
            timerEditor.Tick += timerEditor_Tick;
            timerEditor.IsEnabled = true;
            timerEditor.Stop();
            #endregion

            #region Initialize State Values
            mediaPlayer.Volume = 0;

            AudioStates.Genre = GetSetData.GetSetting("LastGenre");
            //AudioStates.Catalog = GetSetData.GetSetting("LastCatalog");
            AudioStates.Album = GetSetData.GetSetting("LastAlbum");
            AudioStates.Artist = GetSetData.GetSetting("LastArtist");

            _lastGenreID = Convert.ToInt32(GetSetData.GetSetting("LastGenreID"));
            //_lastCatalogID = Convert.ToInt32(GetSetData.GetSetting("LastCatalogID"));
            _lastAlbumID = Convert.ToInt32(GetSetData.GetSetting("LastAlbumID"));

            _lastRow = Convert.ToInt32(GetSetData.GetSetting("LastRow"));
            _lastTab = Convert.ToInt32(GetSetData.GetSetting("LastTab"));
            _lastPlaylistID = Convert.ToInt32(GetSetData.GetSetting("LastPlaylistID"));
            _lastQuery = GetSetData.GetSetting("LastQuery");

            this.Height = Convert.ToInt32(GetSetData.GetSetting("FormHeight"));
            this.Width = Convert.ToInt32(GetSetData.GetSetting("FormWidth"));
            this.Top = Convert.ToInt32(GetSetData.GetSetting("FormTop"));
            this.Left = Convert.ToInt32(GetSetData.GetSetting("FormLeft"));

            expanderLeftPanel.IsExpanded = Convert.ToBoolean(GetSetData.GetSetting("LeftPanel"));
            textboxQuery.Tag = GetSetData.GetSetting("PlaceHolder");
            #endregion
        }
        #endregion

        private enum DataSource
        {
            Songs,
            Playlist,
            Query
        }

        private enum Tab
        {
            Audio,
            Playlist
        }

        #region FormEvents
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string rootFolder = "";

            InitializeComboGenres();

            FillPlaylists();
            FillQueryCombo();

            tabcontrol.SelectedIndex = _lastTab;

            if (!String.IsNullOrEmpty(_lastQuery))
            {
                _lastRow = GetSetData.GetQueryLastRow(_lastQuery);
                FillDatagridByQuery();
            }
            else
            {
                if (tabcontrol.SelectedIndex == (int)Tab.Audio)
                {
                    _lastRow = GetSetData.GetAlbumLastRow(AudioStates.Album);
                    FillDatagridByTabAudio();

                }
                else
                {
                    FillDatagridByTabPlaylist();
                }
            }

            DatagridContextmenuCreate();

            if (datagrid.ItemsSource != null)
            {
                datagrid.SelectedIndex = _lastRow;
                datagrid.SelectedItem = _lastRow;
                datagrid.ScrollIntoView(datagrid.SelectedItem);
                datagrid.Focus();

                if (GetSetData.Datasource == GetSetData.DataSourceEnum.Songs ||
                    GetSetData.Datasource == GetSetData.DataSourceEnum.Query)
                {
                    var record = (vSongModel)datagrid.SelectedItem;
                    rootFolder = Helpers.GetShortPath(record.Path);
                    _artist = record.Artist;
                }

                if (GetSetData.Datasource == GetSetData.DataSourceEnum.Playlist)
                {
                    var record = (vPlaylistSongModel)datagrid.SelectedItem;
                    rootFolder = record.Path;
                    _artist = record.Artist;
                }
                _dataLoaded = true;
            }

            statusGenre.Text = AudioStates.Genre;
            statusCatalog.Text = AudioStates.Catalog;
            statusArtist.Text = AudioStates.Artist;
            statusAlbum.Text = AudioStates.Album;
            sliderVolume.Value = Convert.ToDouble(SettingsDb.Settings["Volume"]);

            ImageFlipperLoad(rootFolder + "\\" + AudioStates.Genre, _isLoaded);

            _isLoaded = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //SaveSettings();
        }

        private void Move_Window(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();

            GetSetData.SetSettingGeneral("FormLeft", this.Left.ToString());
            GetSetData.SetSettingGeneral("FormTop", this.Top.ToString());
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GetSetData.SetSettingGeneral("FormHeight", this.Height.ToString());
            GetSetData.SetSettingGeneral("FormWidth", this.Width.ToString());
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void buttonMaximize_Checked(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
        }

        private void buttonMaximize_Unchecked(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void menuEditRecordLocation_Click(object sender, RoutedEventArgs e)
        {
            Views.InputBox inputBox = new Views.InputBox();
            inputBox.ITitle = "EditRecord Location";
            inputBox.ILabel = "Enter new Path:";
            inputBox.IText = GetSetData.GetSetting("RecordEditorLocation");
            inputBox.IKeepText = true;
            var result = inputBox.ShowDialog();

            if (result == true)
            {
                string path = inputBox.IText;
                // check and save new location
            }

            inputBox = null;

        }

        private void menuEditMultiline_Click(object sender, RoutedEventArgs e)
        {
            datagrid.SelectionMode = DataGridSelectionMode.Extended;
        }

        private void menuToolsTest1_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuToolsTest2_Click(object sender, RoutedEventArgs e)
        {
            player.Ctlcontrols.stop();
        }

        private void menuDatabaseCheckPath_Click(object sender, RoutedEventArgs e)
        {
            Helpers.CheckSongPathExist();
        }

        private void touglebuttonSpeaker_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.IsMuted = (bool)touglebuttonSpeaker.IsChecked;
        }

        private void tabcontrol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoaded == false)
                return;

            _lastTab = tabcontrol.SelectedIndex;
            GetSetData.SetSettingGeneral("LastTab", _lastTab.ToString());

            mediaPlayer.Stop();
            _mediaPlayerIsPlaying = false;

            DatagridContextmenuCreate();

            if (_lastTab == 0)
                FillDatagridByTabAudio();
            else
                FillDatagridByTabPlaylist();

        }

        private void listboxGenres_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _lastRow = 0;

            var item = (GenreModel)listboxGenres.SelectedItem;

            _lastGenreID = item.ID;
            AudioStates.Genre = item.Name;

            _lastCatalogID = item.LastCatalogID;
            AudioStates.Catalog = item.LastCatalog;

            GetSetData.SetGenreCatalog(_lastGenreID, item.LastCatalog, item.LastCatalogID);

            GetSetData.SetSettingGeneral("LastGenre", AudioStates.Genre);
            GetSetData.SetSettingGeneral("LastGenreID", _lastGenreID.ToString());

            FillListboxCatalogs();

            statusGenre.Text = AudioStates.Genre;
            FillDatagridByTabAudio();
        }

        private void listboxCatalogs_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = (CatalogModel)listboxCatalogs.SelectedItem;

            GetSetData.SetGenreCatalog(_lastGenreID, item.Name, item.ID);

            if (item != null)
            {
                AudioStates.Catalog = item.Name;
                _lastCatalogID = GetSetData.GetCatalogID(AudioStates.Catalog);

                _lastAlbumID = 0;
                AudioStates.Album = "Alle";

                GetSetData.SetSettingGeneral("LastCatalog", AudioStates.Catalog);
                GetSetData.SetSettingGeneral("LastCatalogID", _lastCatalogID.ToString());
                GetSetData.SetSettingGeneral("LastAlbum", AudioStates.Album);
                GetSetData.SetSettingGeneral("LastAlbumID", _lastAlbumID.ToString());

                FillListboxAlbums();
            };

            if (listboxAlbums.Items.Count > 0)
            {
                listboxAlbums.SelectedIndex = 0;
                listboxAlbums.SelectedItem = 0;
            }

            FillDatagridByTabAudio();
        }

        private void listboxAlbums_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = (AlbumModel)listboxAlbums.SelectedItem;

            AudioStates.Album = item.Name;
            _lastAlbumID = item.ID;

            GetSetData.SetSettingGeneral("LastAlbum", AudioStates.Album);
            GetSetData.SetSettingGeneral("LastAlbumID", _lastAlbumID.ToString());
            GetSetData.SetSettingGeneral("LastArtist", "Alle");
            GetSetData.SetSettingAlbumLastRow("Name", 0);

            statusAlbum.Text = AudioStates.Album;

            FillListboxArtists();
            FillDatagridByTabAudio();
        }

        private void listboxArtists_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            string artist = listboxArtists.SelectedItem.ToString();

            AudioStates.Artist = artist;
            _lastArtist = artist;

            GetSetData.SetSettingGeneral("LastArtist", AudioStates.Artist);

            statusArtist.Text = AudioStates.Artist;

            FillDatagridByTabAudio();
        }

        private void listboxPlaylists_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PlaylistModel playlist = listboxPlaylists.SelectedItem as PlaylistModel;
            GetSetData.SetSettingGeneral("LastPlaylist", playlist.Name);
            GetSetData.SetSettingGeneral("LastPlaylistID", playlist.ID.ToString());

            _lastPlaylistID = playlist.ID;
            _lastPlaylist = playlist.Name;

            FillDatagridByTabPlaylist();
        }

        private void contextmenuPlaylistAdd_Click(object sender, RoutedEventArgs e)
        {
            Views.InputBox inputBox = new Views.InputBox();
            inputBox.ITitle = "New Playlist";
            inputBox.ILabel = "Enter Playlist name:";
            inputBox.IText = "<Playlistname>";
            var result = inputBox.ShowDialog();

            if (result == true)
            {
                try
                {
                    string plname = inputBox.IText;
                    int lastid = GetSetData.PlaylisAddNew(plname);

                    PlaylistModel pl = GetSetData.GetPlaylist(lastid);
                    _playlists.Add(pl);
                }
                catch (Exception ex)
                {
                    Views.MyMessageBox messageBox = new Views.MyMessageBox();
                    messageBox.MTitle = "New Playlist";
                    messageBox.MMessage = "PlaylistAdd:\n" + ex.Message + "\n" + ex.StackTrace;
                    messageBox.ShowDialog();
                }
            }

            inputBox = null;
        }

        private void contextmenuPlaylistRemove_Click(object sender, RoutedEventArgs e)
        {
            var playlist = (PlaylistModel)listboxPlaylists.SelectedItem;
            int id = playlist.ID;

            try
            {
                GetSetData.PlaylistRemove(id);
                _playlists.Remove(playlist);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void contextmenuPlaylistRename_Click(object sender, RoutedEventArgs e)
        {
            var playlist = (PlaylistModel)listboxPlaylists.SelectedItem;
            int index = listboxPlaylists.SelectedIndex;

            Views.InputBox inputBox = new Views.InputBox();
            inputBox.ITitle = "New Playlist";
            inputBox.ILabel = "Enter Playlist name:";
            inputBox.IText = playlist.Name;
            inputBox.IKeepText = true;
            var result = inputBox.ShowDialog();

            if (result == true)
            {
                try
                {
                    string plname = inputBox.IText;

                    GetSetData.PlaylistRename(playlist.ID, plname);

                    List<PlaylistModel> list = GetSetData.GetAllPlaylists();

                    _playlists = null;
                    _playlists = new ObservableCollection<PlaylistModel>(list);

                    listboxPlaylists.ItemsSource = null;
                    listboxPlaylists.ItemsSource = _playlists;
                    listboxPlaylists.SelectedIndex = index;
                    listboxPlaylists.SelectedItem = index;


                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        #endregion

        #region Methods
        void InitMediaPlayer()
        {
            player = formsHost.Child as AxWMPLib.AxWindowsMediaPlayer;
            player.uiMode = "none";
            player.settings.autoStart = false;
            //player.settings.setMode("loop", false);
            //player.stretchToFit = true;
            //player.enableContextMenu = false;
            player.AllowDrop = true;
            player.ErrorEvent += new EventHandler(player_ErrorEvent);
            player.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(player_PlayStateChange);
            player.ClickEvent += new AxWMPLib._WMPOCXEvents_ClickEventHandler(player_ClickEvent);
        }

        void player_ErrorEvent(object sender, EventArgs e)
        {
            //textblockStatus.Text = "An error occured while trying to play the video.";
        }

        void player_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (e.newState == (int)WMPLib.WMPPlayState.wmppsMediaEnded)
            {
                Debug.Print("media ended");
            }

            if (e.newState == (int)WMPLib.WMPPlayState.wmppsPlaying)
            {
                //_duration = player.currentMedia.duration;
                //sliderPosition.Maximum = (int)_duration;
                //player.settings.volume = (int)sliderVolume.Value;
                //var ar = player.URL.Split('\\');
                //textblockStatus.Text = ar[ar.Length - 1].Replace(".mp3", "");
            }
        }

        void player_ClickEvent(object sender, AxWMPLib._WMPOCXEvents_ClickEvent e)
        {

        }

        private void SaveSettings()
        {
            try
            {
                SettingsDb.Settings["LastGenre"] = AudioStates.Genre;
                SettingsDb.Settings["LastGenreID"] = Convert.ToString(_lastGenreID);
                SettingsDb.Settings["LastCatalog"] = AudioStates.Catalog;
                SettingsDb.Settings["LastCatalogID"] = Convert.ToString(_lastCatalogID);
                SettingsDb.Settings["LastArtist"] = AudioStates.Artist;
                SettingsDb.Settings["LastAlbum"] = AudioStates.Album;
                SettingsDb.Settings["LastAlbumID"] = Convert.ToString(_lastAlbumID);
                SettingsDb.Settings["LastTab"] = _lastTab.ToString();
                SettingsDb.Settings["Volume"] = Convert.ToString(mediaPlayer.Volume);
                SettingsDb.Settings["LastRow"] = Convert.ToString(_lastRow);
                SettingsDb.Settings["FormHeight"] = this.Height.ToString();
                SettingsDb.Settings["FormWidth"] = this.Width.ToString();
                SettingsDb.Settings["FormTop"] = this.Top.ToString();
                SettingsDb.Settings["FormLeft"] = this.Left.ToString();
                SettingsDb.Settings["LeftPanel"] = expanderLeftPanel.IsExpanded.ToString();
                //SettingsDb.Save();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        private bool hasDataGridErrors()
        {
            var rowIndex = datagrid.SelectedIndex;

            DataGridRow row = (DataGridRow)datagrid.ItemContainerGenerator.ContainerFromIndex(rowIndex);

            if (row != null && Validation.GetHasError(row))
                return true;
            else
                return false;
        }

        private void ImageFlipperLoad(string currentpath, bool firstload)
        {
            imageArtist.Tag = 0;
            _artistImageList = ImageFlipper.GetImagesFullNames(Helpers.GetImagePath(), _artist);
        }

        private void FillDatagridByTabAudio()
        {
            _dataLoaded = false;

            List<vSongModel> results = GetSetData.GetTitles(_lastGenreID, _lastCatalogID, AudioStates.Album, AudioStates.Artist);

            if (results != null && results.Count > 0)
            {
                GetSetData.Datasource = GetSetData.DataSourceEnum.Songs;
                datagrid.ItemsSource = results;
                _dataLoaded = true;

                string album = AudioStates.Album;
                _lastRow = album == "Alle" ? 0 : GetSetData.GetAlbumLastRow(AudioStates.Album);

                if (datagrid.Items.Count > 0)
                {
                    datagrid.SelectedIndex = _lastRow;
                    datagrid.SelectedItem = _lastRow;
                    datagrid.ScrollIntoView(datagrid.SelectedItem);
                }
            }
            else
            {
                datagrid.ItemsSource = null;
            }
        }

        private void FillDatagridByTabPlaylist()
        {
            _dataLoaded = false;
            var results = GetSetData.GetPlaylistEntries(_lastPlaylistID);
            if (results.Count > 0)
            {
                GetSetData.Datasource = GetSetData.DataSourceEnum.Playlist;
                datagrid.ItemsSource = results;
                _dataLoaded = true;

                _lastRow = GetSetData.GetPlaylistLastRow(_lastPlaylistID);

                if (datagrid.Items.Count > 0)
                {
                    datagrid.SelectedIndex = _lastRow;
                    datagrid.SelectedItem = _lastRow;
                    datagrid.ScrollIntoView(datagrid.SelectedItem);
                }
                else
                {
                    datagrid.ItemsSource = null;
                }
            }
            else
            {
                datagrid.ItemsSource = null;
            }
        }

        private void FillDatagridByQuery()
        {
            _dataLoaded = false;
            if (textboxQuery.Text != "")
            {
                List<vSongModel> results = GetSetData.GetQueryResult(textboxQuery.Text);

                if (results != null)
                {
                    GetSetData.Datasource = GetSetData.DataSourceEnum.Query;
                    datagrid.ItemsSource = results;

                    //random.InitRandomNumbers(datagrid.Items.Count - 1);

                    _lastRow = GetSetData.GetQueryLastRow((string)comboboxStoredQueries.SelectedItem);

                    if (datagrid.Items.Count > 1)
                        datagrid.SelectedIndex = _lastRow;
                }
            }
            else
                QueryClear();

            _dataLoaded = true;
        }

        private void SetAlbumRow(string name, int row)
        {
            bool result = GetSetData.SetSettingAlbumLastRow(name, row);
        }

        private void SetPlaylistRow(string name, int row)
        {
            bool result = GetSetData.SetPlaylistLastRow(name, row);
        }

        private void SetQueryRow(string name, int row)
        {
            bool result = GetSetData.SetQueryLastRow(textboxQuery.Text, _lastRow);
        }

        private void InitializeComboGenres()
        {
            List<GenreModel> genres = GetSetData.GetGenres();
            genres.Insert(0, new GenreModel { ID = 0, Name = "Alle" });

            listboxGenres.ItemsSource = genres;
            listboxGenres.SelectedIndex = _lastGenreID;
            listboxGenres.SelectedItem = _lastGenreID;
            listboxGenres.ScrollIntoView(listboxGenres.SelectedItem);
            listboxGenres.Focus();

            var item = (GenreModel)listboxGenres.SelectedItem;
            _lastCatalogID = item.LastCatalogID;
            AudioStates.Catalog = item.LastCatalog;

            FillListboxCatalogs();
        }

        private void FillListboxCatalogs()
        {
            List<CatalogModel> catalogs = GetSetData.GetCatalogs(_lastGenreID);
            catalogs.Insert(0, new CatalogModel { ID = 0, Name = "Alle" });
            listboxCatalogs.ItemsSource = catalogs;

            int index = GetCatalogIndex(AudioStates.Catalog);

            listboxCatalogs.SelectedIndex = index;
            listboxCatalogs.SelectedItem = index;
            listboxCatalogs.ScrollIntoView(listboxCatalogs.SelectedItem);
            listboxCatalogs.Focus();

            FillListboxAlbums();
        }

        private void FillListboxAlbums()
        {
            List<AlbumModel> albums;

            if (_lastCatalogID == 0)
                albums = GetSetData.GetAlbumsByGenreID(_lastGenreID);
            else
                albums = GetSetData.GetAlbums(_lastGenreID, _lastCatalogID);

            if (albums.Count > 1)
                albums.Insert(0, new AlbumModel { ID = 0, Name = "Alle", ID_Genre = _lastGenreID, ID_Catalog = _lastCatalogID });

            listboxAlbums.ItemsSource = albums;
            int index = GetAlbumIndex(AudioStates.Album);

            listboxAlbums.SelectedIndex = index;
            listboxAlbums.SelectedItem = index;
            listboxAlbums.ScrollIntoView(listboxAlbums.SelectedItem);
            listboxAlbums.Focus();

            FillListboxArtists();
        }

        private void FillListboxArtists()
        {
            List<string> artists = GetSetData.GetArtists(AudioStates.Genre, AudioStates.Catalog, AudioStates.Album);

            if (artists.Count > 1)
                artists.Insert(0, "Alle");

            _lastArtist = GetSetData.GetSetting("LastArtist");
            AudioStates.Artist = _lastArtist;

            listboxArtists.ItemsSource = artists;

            int index = GetArtistIndex(AudioStates.Artist);

            listboxArtists.SelectedIndex = index;
            listboxArtists.SelectedItem = index;
            listboxArtists.ScrollIntoView(listboxArtists.SelectedItem);
            listboxArtists.Focus();

        }

        private void FillPlaylists()
        {
            List<PlaylistModel> list = GetSetData.GetAllPlaylists();

            _playlists = new ObservableCollection<PlaylistModel>(list);

            ListBox lb = listboxPlaylists;
            lb.ItemsSource = _playlists;
            lb.SelectedIndex = GetPlaylistSelectedIndex(_lastPlaylistID);
            lb.ScrollIntoView(lb.SelectedItem);
        }

        private int GetPlaylistSelectedIndex(int plid)
        {
            int listindex = -1;

            foreach (PlaylistModel pl in listboxPlaylists.Items)
            {
                listindex += 1;
                if (pl.ID == plid)
                    break;
            }

            return listindex;
        }

        private int GetCatalogIndex(string catalog)
        {
            int listindex = -1;

            foreach (CatalogModel c in listboxCatalogs.Items)
            {
                listindex += 1;
                if (c.Name == catalog)
                    break;
            }
            return listindex;
        }

        private int GetAlbumIndex(string album)
        {
            int listindex = -1;

            foreach (AlbumModel c in listboxAlbums.Items)
            {
                listindex += 1;
                if (c.Name == album)
                    break;
            }
            return listindex;
        }

        private int GetArtistIndex(string artist)
        {
            int listindex = -1;

            foreach (string a in listboxArtists.Items)
            {
                listindex += 1;
                if (a == artist)
                    break;
            }
            return listindex;
        }
        #endregion

        #region Datagrid
        private void datagrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string fullpath = "";
            string path = "";

            statusProgress.Text = @"00:00";
            statusDuration.Text = @"00:00";
            this.Title = "MyJukeboxWMPDapper";

            if (_dataLoaded == false || datagrid.Items.Count == 0)
                return;

            if (datagrid.SelectedItem == null)
            {
                datagrid.SelectedIndex = _lastRow;
                datagrid.CurrentItem = _lastRow;
            }

            if (GetSetData.Datasource == GetSetData.DataSourceEnum.Songs ||
                GetSetData.Datasource == GetSetData.DataSourceEnum.Query)
            {
                var record = (vSongModel)datagrid.SelectedItem;

                _lastID = record.ID;
                _artist = record.Artist;
                path = record.Path;
                fullpath = $"{record.Path}\\{record.FileName}";
                this.Title = $"{record.Artist} - {record.Title}";
            }

            if (GetSetData.Datasource == GetSetData.DataSourceEnum.Playlist)
            {
                var record = (vPlaylistSongModel)datagrid.SelectedItem;
                _lastID = record.ID;
                _artist = record.Artist;
                path = record.Path;

                if (record != null)
                {
                    fullpath = $"{record.Path}\\{record.FileName}";
                    this.Title = $"{record.Artist} - {record.Title}";
                }
            }

            _lastRow = datagrid.SelectedIndex;

            if (GetSetData.Datasource == GetSetData.DataSourceEnum.Songs)
                SetAlbumRow(AudioStates.Album, _lastRow);

            if (GetSetData.Datasource == GetSetData.DataSourceEnum.Query)
                SetQueryRow(textboxQuery.Text, _lastRow);

            if (GetSetData.Datasource == GetSetData.DataSourceEnum.Playlist)
                SetPlaylistRow(_lastPlaylist, _lastRow);

            var root = Helpers.GetShortPath(fullpath);
            ImageFlipperLoad(root + "\\" + AudioStates.Genre, _isLoaded);

            mediaPlayer.Source = new Uri(fullpath);

            //player.URL = fullpath;
            //player.settings.volume = 80;
            //player.Ctlcontrols.play();
        }

        #endregion

        #region Slider Events
        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_dataLoaded)
            {
                mediaPlayer.Volume = sliderVolume.Value;
                player.settings.volume = (int)sliderVolume.Value;
            }
        }
        private void sliderVolume_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var vol = (e.Delta > 0) ? 0.1 : -0.1;
            sliderVolume.Value += vol;
            mediaPlayer.Volume += vol;
        }

        private void sliderPosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            statusProgress.Text = TimeSpan.FromSeconds(sliderPosition.Value).ToString(@"mm\:ss");
        }

        private void sliderPosition_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _userIsDraggingSlider = true;
        }

        private void sliderPosition_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _userIsDraggingSlider = false;
            mediaPlayer.Position = TimeSpan.FromSeconds(sliderPosition.Value);
        }

        #endregion

        #region Query Elements
        private void buttonQueryClear_Click(object sender, RoutedEventArgs e)
        {
            QueryClear();
        }

        private void QueryClear()
        {
            textboxQuery.Text = comboboxStoredQueries.Text = "";
            GetSetData.SetSettingGeneral("LastQuery", "");

            if (tabcontrol.SelectedIndex == (int)Tab.Audio)
                FillDatagridByTabAudio();
            else
                FillDatagridByTabPlaylist();
        }

        private void buttonQuerySearch_Click(object sender, RoutedEventArgs e)
        {
            FillDatagridByQuery();
        }

        private void buttonQuerySave_Click(object sender, RoutedEventArgs e)
        {
            AddQueryToComboBox(textboxQuery.Text);
        }

        private void AddQueryToComboBox(string query)
        {
            if (_queries != null)
            {
                var itemExist = _queries.Any(q => q.Contains(query));
                if (itemExist == false)
                {
                    bool result = GetSetData.QueryAdd(query);
                    if (result == true)
                        _queries.Add(query);
                }
            }
            else
            {
                _queries.Add(query);
            }
        }

        public void FillQueryCombo()
        {
            List<string> list = null;
            _queries = new ObservableCollection<string>();

            list = GetSetData.QueryGetList();

            if (list.Count > 0)
            {
                list.Insert(0, "");

                _queries = new ObservableCollection<string>(list);
                comboboxStoredQueries.ItemsSource = _queries;
                comboboxStoredQueries.Text = _lastQuery;
                textboxQuery.Text = _lastQuery;
            }
        }

        private void textboxQuery_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                FillDatagridByQuery();
            else if (e.Key == Key.Escape)
            {
                textboxQuery.Text = "";
                datagrid.Focus();
            }
        }

        private void comboboxStoredQueries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoaded == false)
                return;

            timerDuration.Stop();
            mediaPlayer.Stop();
            textboxQuery.Text = (string)comboboxStoredQueries.SelectedItem;
            GetSetData.SetSettingGeneral("LastQuery", textboxQuery.Text);
            FillDatagridByQuery();
        }

        private void buttonQueryDelete_Click(object sender, RoutedEventArgs e)
        {
            string query = (string)comboboxStoredQueries.SelectedItem;

            bool result = GetSetData.QueryRemove(comboboxStoredQueries.Text);

            if (result == true)
                _queries.Remove(query);
        }
        #endregion

        #region MediaPlayer Elements

        #region Commands
        private void Play_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (mediaPlayer != null) && (mediaPlayer.Source != null);
        }

        private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_artistImageList == null || _artistImageList.Count == 0)
            {
                Uri uri = new Uri("/Images/ShadowMen.gif", UriKind.Relative);
                BitmapImage image = new BitmapImage(uri);
                imageArtist.Source = image;
                imageArtist.Stretch = System.Windows.Media.Stretch.Uniform;
            }

            timerDuration.Start();
            timerFlipImage.Start();

            mediaPlayer.Play();
            _mediaPlayerIsPlaying = true;
            statusDuration.Text = TimeSpan.FromSeconds(sliderPosition.Maximum).ToString(@"mm\:ss");
            sliderPosition.TickFrequency = sliderPosition.Maximum / 10;
        }

        private void Pause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _mediaPlayerIsPlaying;
        }

        private void Pause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mediaPlayer.Pause();
            timerDuration.Stop();
            timerFlipImage.Stop();
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _mediaPlayerIsPlaying;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mediaPlayer.Stop();
            _mediaPlayerIsPlaying = false;
            timerDuration.Stop();
            timerFlipImage.Stop();
        }

        private void CopyDataRowCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CopyDataRowExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            int id = -1;

            if (_lastTab == 1)
            {
                var song = (vPlaylistSongModel)datagrid.SelectedItem;
                id = song.ID;
            }
            else
            {
                var song = (vSongModel)datagrid.SelectedItem;
                id = song.ID;
            }

            string songFields = GetSetData.GetSongFieldValuesByID(id);

            Clipboard.Clear();
            Clipboard.SetText(songFields);
        }

        private void PlaybackLoop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void PlaybackLoop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Playback_Shuffle.IsChecked = false;
        }

        private void PlaybackShuffle_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void PlaybackShuffle_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            Playback_Loop.IsChecked = false;
            random.InitRandomNumbers(datagrid.Items.Count - 1);
        }

        #endregion

        private void mediaplayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            _mediaPlayerIsPlaying = false;

            if (Playback_Shuffle.IsChecked == true)
            {
                var nextRow = random.GetNextNumber;
                datagrid.SelectedIndex = nextRow;
                datagrid.SelectedItem = nextRow;
                datagrid.ScrollIntoView(datagrid.SelectedItem);
                _mediaPlayerIsPlaying = true;
            }
            else if (Playback_Loop.IsChecked == true)
            {
                mediaPlayer.Position = TimeSpan.FromSeconds(0);
                _mediaPlayerIsPlaying = true;
            }
            else
            {
                datagrid.SelectedIndex = GetNextIndex();
                _mediaPlayerIsPlaying = true;
            }
        }

        private int GetNextIndex()
        {
            int newIndex = -1;
            int currentIndex = datagrid.SelectedIndex;

            if (currentIndex + 1 < datagrid.Items.Count)
            {
                datagrid.SelectedIndex = currentIndex + 1;

                while (hasDataGridErrors())
                {
                    datagrid.SelectedIndex = datagrid.SelectedIndex + 1;
                }

                newIndex = datagrid.SelectedIndex;
            }
            else
                newIndex = currentIndex;

            return newIndex;
        }

        #endregion

        #region ContextmenuDatagrid

        private void DatagridContextmenuCreate()
        {
            if (tabcontrol.SelectedIndex == (int)Tab.Audio)
                DatagridContextmenuCreateAudio();

            if (tabcontrol.SelectedIndex == (int)Tab.Playlist)
                DatagridContextmenuCreatePlaylist();
        }

        private void DatagridContextmenuCreateAudio()
        {
            // add menu items to existing menu
            ContextMenu contextmenu = (ContextMenu)this.FindResource("contextmenuDatagrid");
            MenuItem miSendto = (MenuItem)contextmenu.Items[0];
            miSendto.Items.Clear();

            miSendto.Header = "Send to";
            foreach (var playlist in _playlists)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = playlist.Name;
                menuItem.Tag = playlist.ID;
                menuItem.Click += new RoutedEventHandler(this.contextmenuDatagridSendtoPlaylist_Click);
                miSendto.Items.Add(menuItem);
            }

            // remove menuitem 'remove'
            MenuItem mi = (MenuItem)contextmenu.Items[1];
            contextmenu.Items.Remove(mi);

            MenuItem miRemove = new MenuItem();
            miRemove.Header = "Remove";
            miRemove.Click += new RoutedEventHandler(this.contextmenuDatagridRemoveFromAudio_Click);
            contextmenu.Items.Insert(1, miRemove);
        }

        private void DatagridContextmenuCreatePlaylist()
        {
            // add menu items to existing menu
            ContextMenu contextmenu = (ContextMenu)this.FindResource("contextmenuDatagrid");
            MenuItem miSendto = (MenuItem)contextmenu.Items[0];
            miSendto.Items.Clear();

            miSendto.Header = "Move to";
            foreach (var playlist in _playlists)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = playlist.Name;
                menuItem.Tag = playlist.ID;
                menuItem.Click += new RoutedEventHandler(this.contextmenuDatagridMovetoPlaylist_Click);
                miSendto.Items.Add(menuItem);
            }

            // remove menuitem 'remove'
            MenuItem mi = (MenuItem)contextmenu.Items[1];
            contextmenu.Items.Remove(mi);

            MenuItem miRemove = new MenuItem();
            miRemove.Header = "Remove";
            miRemove.Click += new RoutedEventHandler(this.contextmenuDatagridRemoveFromPlaylist_Click);
            contextmenu.Items.Insert(1, miRemove);
        }

        private void contextmenuDatagridMovetoPlaylist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var item in datagrid.SelectedItems)
                {
                    var rowlist = (vPlaylistSongModel)item;
                    int songId = rowlist.ID;

                    var menuitem = sender as MenuItem;
                    int plidnew = (int)menuitem.Tag;

                    GetSetData.PlaylistEntryMove(songId, _lastPlaylistID, plidnew);
                }

                FillDatagridByTabPlaylist();
            }
            catch (Exception ex)
            {
                Views.MyMessageBox myMessageBox = new Views.MyMessageBox();
                myMessageBox.MTitle = "Error:PlaylistEntryMove";
                myMessageBox.MMessage = ex.Message + "\n" + ex.StackTrace;
                myMessageBox.ShowDialog();

            }
        }

        private void contextmenuDatagridSendtoPlaylist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var item in datagrid.SelectedItems)
                {
                    var rowlist = (vSongModel)item;
                    int songId = rowlist.ID;
                    GetSetData.AddSongToPlaylist(songId, _lastPlaylistID);
                }
            }
            catch (Exception ex)
            {
                Views.MyMessageBox myMessageBox = new Views.MyMessageBox();
                myMessageBox.MTitle = "Error:AddSongToPlaylist";
                myMessageBox.MMessage = ex.Message + "\n" + ex.StackTrace;
                myMessageBox.ShowDialog();

            }
        }

        private void datagridMenuitemOpenEditor_Click(object sender, RoutedEventArgs e)
        {
            int _lastRow = datagrid.SelectedIndex;
            processEnded = false;

            string ids = "";

            try
            {
                var records = datagrid.SelectedItems;

                foreach (vSongModel row in records)
                {
                    ids += row.ID.ToString() + " ";
                }

                StartRecordEditor startRecordEditor = new StartRecordEditor();
                startRecordEditor.DefineProcess(ids);
                timerEditor.Start();

                if (GetSetData.Datasource == GetSetData.DataSourceEnum.Songs)
                    FillDatagridByTabAudio();

                if (GetSetData.Datasource == GetSetData.DataSourceEnum.Query)
                    FillDatagridByQuery();

                if (GetSetData.Datasource == GetSetData.DataSourceEnum.Playlist)
                    FillDatagridByTabPlaylist();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR: Open Record Editor");
            }
        }

        private void OpenFolder_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (_lastTab == (int)Tab.Audio)
                {
                    var rowlist = (vSongModel)datagrid.SelectedItem;
                    Process.Start(rowlist.Path);
                }

                if (_lastTab == (int)Tab.Playlist)
                {
                    var rowlist = (vPlaylistSongModel)datagrid.SelectedItem;
                    Process.Start(rowlist.Path);
                }
            }
            catch { }
        }

        private void datagridMenuitemCopyCell_Click(object sender, RoutedEventArgs e)
        {

        }

        private void contextmenuDatagridRemoveFromAudio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var item in datagrid.SelectedItems)
                {
                    var rowlist = (vSongModel)item;
                    int songId = rowlist.ID;
                    var result = GetSetData.DeleteSong(songId);
                }

                datagrid.SelectionMode = DataGridSelectionMode.Single;
                FillDatagridByTabAudio();
            }
            catch (Exception ex)
            {
                Views.MyMessageBox myMessageBox = new Views.MyMessageBox();
                myMessageBox.MTitle = "Error: Remove Song from Audio failed!";
                myMessageBox.MMessage = ex.Message + "\n" + ex.StackTrace;
                myMessageBox.ShowDialog();
            }
        }

        private void contextmenuDatagridRemoveFromPlaylist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var item in datagrid.SelectedItems)
                {
                    var rowlist = (vPlaylistSongModel)item;
                    int songId = rowlist.ID;

                    GetSetData.RemoveEntryFromPlaylist(songId, _lastPlaylistID);
                }

                FillDatagridByTabPlaylist();
            }
            catch (Exception ex)
            {
                Views.MyMessageBox myMessageBox = new Views.MyMessageBox();
                myMessageBox.MTitle = "Error: RemoveEntryFromPlaylist";
                myMessageBox.MMessage = ex.Message + "\n" + ex.StackTrace;
                myMessageBox.ShowDialog();
            }
        }

        #endregion

        #region Timer Methods
        private void SetArtistImage(string path)
        {
            Uri uri;
            BitmapImage image;

            uri = new Uri(path, UriKind.RelativeOrAbsolute);
            image = new BitmapImage(uri);
            imageArtist.Source = image;
            imageArtist.Stretch = System.Windows.Media.Stretch.Uniform;
        }

        private void EditorEnded()
        {
            if (GetSetData.Datasource == GetSetData.DataSourceEnum.Songs)
                FillDatagridByTabAudio();

            if (GetSetData.Datasource == GetSetData.DataSourceEnum.Query)
                FillDatagridByQuery();

            if (GetSetData.Datasource == GetSetData.DataSourceEnum.Playlist)
                FillDatagridByTabPlaylist();

            datagrid.SelectedIndex = _lastRow;
        }

        #endregion

        #region Timers
        private void timerDuration_Tick(object sender, EventArgs e)
        {
            if ((mediaPlayer.Source != null) && (mediaPlayer.NaturalDuration.HasTimeSpan) && (!_userIsDraggingSlider))
            {
                sliderPosition.Minimum = 0;
                sliderPosition.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                sliderPosition.Value = mediaPlayer.Position.TotalSeconds;
                sliderPosition.TickFrequency = sliderPosition.Maximum / 10;
                statusDuration.Text = TimeSpan.FromSeconds(sliderPosition.Maximum).ToString(@"mm\:ss");
            }
        }

        private void timerFlipImage_Tick(object sender, EventArgs e)
        {
            if (_artistImageList.Count == 0)
            {
                SetArtistImage("/Images/ShadowMen.gif");
                return;
            }

            if (Tag == null || (int)Tag >= _artistImageList.Count) Tag = 0;
            int counter = (int)Tag;

            SetArtistImage(_artistImageList[counter]);

            Tag = ++counter;
        }

        private void timerEditor_Tick(object sender, EventArgs e)
        {
            if (processEnded == true)
            {
                processEnded = false;
                EditorEnded();
                timerEditor.Stop();
            }
        }
        #endregion

    }
}
