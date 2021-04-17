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
        private int _lastPlaylistID = -1;
        private string _lastPlaylist = "";
        private string _lastQuery = "";
        private string _artist = "";
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

            GetSetData.SetSetting("FormLeft", this.Left.ToString());
            GetSetData.SetSetting("FormTop", this.Top.ToString());
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GetSetData.SetSetting("FormHeight", this.Height.ToString());
            GetSetData.SetSetting("FormWidth", this.Width.ToString());
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            //SaveSettings();
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
            //SaveSettings();
            this.Close();
        }

        private void menuEditRecordLocation_Click(object sender, RoutedEventArgs e)
        {
            Window input = new Views.InputBox();
            input.Show();
            input = null;
        }

        private void menuEditMultiline_Click(object sender, RoutedEventArgs e)
        {
            datagrid.SelectionMode = DataGridSelectionMode.Extended;
        }

        private void menuToolsTest1_Click(object sender, RoutedEventArgs e)
        {
            //List<string> artists = GetSetData.GetArtists(AudioStates.Genre, AudioStates.Catalog, AudioStates.Album);
            //listboxArtists.ItemsSource = artists;

            //if (!ApplicationDeployment.IsNetworkDeployed)
            //{
            //    MessageBox.Show(string.Format("Your application name - v{0}",
            //        ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString(4))
            //        );
            //}

            //statusVersion.Text = ConfigurationManager.AppSettings.GetKey["Version"]

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
            GetSetData.SetSetting("LastTab", _lastTab.ToString());

            mediaPlayer.Stop();
            _mediaPlayerIsPlaying = false;

            DatagridContextmenuCreate();

            if (_lastTab == 0)
                FillDatagridByTabAudio();
            else
                FillDatagridByTabPlaylist();

        }

        private void listboxGenres_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _lastRow = 0;

            var item = (GenreModel)listboxGenres.SelectedItem;

            _lastGenreID = item.ID;
            AudioStates.Genre = item.Name;

            _lastCatalogID = item.LastCatalogID;
            AudioStates.Catalog = item.LastCatalog;

            GetSetData.SetGenreCatalog(_lastGenreID, item.LastCatalog, item.LastCatalogID);

            GetSetData.SetSetting("LastGenre", AudioStates.Genre);
            GetSetData.SetSetting("LastGenreID", _lastGenreID.ToString());

            FillListboxCatalogs();

            statusGenre.Text = AudioStates.Genre;
            FillDatagridByTabAudio();
        }

        private void listboxCatalogs_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = (CatalogModel)listboxCatalogs.SelectedItem;

            GetSetData.SetGenreCatalog(_lastGenreID, item.Name, item.ID);

            if (item != null)
            {
                AudioStates.Catalog = item.Name;
                _lastCatalogID = GetSetData.GetCatalogID(AudioStates.Catalog);

                //AudioStates.Catalog = _lastCatalog == "Alle" ? "" : _lastCatalog;

                _lastAlbumID = 0;
                AudioStates.Album = "Alle";

                //AudioStates.Album = _lastAlbum == "Alle" ? "" : _lastAlbum;

                GetSetData.SetSetting("LastCatalog", AudioStates.Catalog);
                GetSetData.SetSetting("LastCatalogID", _lastCatalogID.ToString());
                GetSetData.SetSetting("LastAlbum", AudioStates.Album);
                GetSetData.SetSetting("LastAlbumID", _lastAlbumID.ToString());

                FillListboxAlbums();
            };

            if (listboxAlbums.Items.Count > 0)
            {
                listboxAlbums.SelectedIndex = 0;
                listboxAlbums.SelectedItem = 0;
            }

            //var album = (AlbumModel)listboxAlbums.SelectedItem;
            //AudioStates.Album = album.Name == "Alle" ? "" : album.Name;
            //statusAlbum.Text = album.Name;


            FillDatagridByTabAudio();
        }

        private void listboxArtists_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string artist = listboxArtists.SelectedItem.ToString();
            AudioStates.Artist = artist == "Alle" ? "" : artist;
            //FillAlbumsAsync();
            statusArtist.Text = artist;
            FillDatagridByTabAudio();
        }

        private void listboxAlbums_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = (AlbumModel)listboxAlbums.SelectedItem;

            AudioStates.Album = item.Name;
            _lastAlbumID = item.ID;
            statusAlbum.Text = AudioStates.Album;

            FillDatagridByTabAudio();
        }

        private void listboxPlaylists_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PlaylistModel playlist = listboxPlaylists.SelectedItem as PlaylistModel;
            GetSetData.SetSetting("LastPlaylist", playlist.Name);
            GetSetData.SetSetting("LastPlaylistID", playlist.ID.ToString());

            _lastPlaylistID = playlist.ID;
            _lastPlaylist = playlist.Name;

            FillDatagridByTabPlaylist();
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

        //private void CheckSongPathExist()
        //{
        //    var pathlist = DataGetSet.GetSongPathList();

        //    bool found = true;

        //    if (pathlist != null)
        //    {
        //        foreach (var p in pathlist)
        //        {
        //            Debug.Print($"path={p}");
        //            if (!Directory.Exists(p))
        //                found = false;
        //        }
        //    }
        //    else
        //        found = false;

        //    if (found == false)
        //    {
        //        Views.MyMessageBox messageBox = new Views.MyMessageBox();
        //        messageBox.MTitle = "Check Song Path";
        //        messageBox.MMessage = "No song path found!\n\nExit Application.";

        //        var result = messageBox.ShowDialog();

        //        Application.Current.Shutdown();
        //    }
        //}

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

        // ToDo refactor fill datagrid
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

                if (datagrid.Items.Count > 1)
                {
                    datagrid.SelectedIndex = _lastRow;
                    datagrid.SelectedItem = _lastRow;
                }

                datagrid.ScrollIntoView(datagrid.SelectedItem);
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

                if (datagrid.Items.Count > 1)
                {
                    datagrid.SelectedIndex = _lastRow;
                    datagrid.SelectedItem = _lastRow;
                }

                datagrid.ScrollIntoView(datagrid.SelectedItem);
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
            bool result = GetSetData.SetAlbumLastRow(name, row);
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
            List<GenreModel> genres = DataAccess.GetSetData.GetGenres();
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
            List<AlbumModel> albums;     // = new AlbumModel();

            if (_lastCatalogID == 0)
                albums = GetSetData.GetAlbumsByGenreID(_lastGenreID);
            else
                albums = GetSetData.GetAlbums(_lastGenreID, _lastCatalogID);

            if (albums.Count > 1)
                albums.Insert(0, new AlbumModel { ID = 0, Name = "Alle", ID_Genre = _lastGenreID, ID_Catalog = _lastCatalogID });

            listboxAlbums.ItemsSource = albums;
            listboxAlbums.SelectedIndex = _lastAlbumID;
            listboxAlbums.SelectedItem = _lastAlbumID;
            listboxAlbums.ScrollIntoView(listboxAlbums.SelectedItem);
            listboxAlbums.Focus();

            FillArtists();
        }

        private void FillArtists()
        {
            List<string> artists = GetSetData.GetArtists(AudioStates.Genre, AudioStates.Catalog, AudioStates.Album);
            listboxArtists.ItemsSource = artists;
        }

        private void FillPlaylists()
        {
            List<PlaylistModel> list = GetSetData.GetPlaylists();

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

        //private async Task FillListboxesAsync()
        //{
        //    await FillGenresAsync();
        //    await FillCatalogsAsync();
        //    await FillArtistsAsync();
        //    await FillAlbumsAsync();
        //}

        //private async Task FillPlaylistsAsync()
        //{
        //    //List<Playlist> list = await DataGetSet.GetPlaylistsAsync();

        //    //_playlists = new ObservableCollection<Playlist>(list);

        //    //ListBox lb = listboxPlaylists;
        //    //lb.ItemsSource = _playlists;
        //    //lb.SelectedIndex = _lastPlaylist;
        //    //lb.ScrollIntoView(lb.SelectedItem);
        //}

        //private async Task FillGenresAsync()
        //{
        //    List<string> list = await DataGetSet.GetGenresAsync();

        //    _genres = new ObservableCollection<string>(list);
        //    _genres.Insert(0, "Alle");

        //    ListBox lb = listboxGenres;
        //    lb.ItemsSource = _genres;
        //    lb.SelectedItem = AudioStates.Genre;
        //    lb.ScrollIntoView(lb.SelectedItem);
        //}

        //private async Task FillCatalogsAsync()
        //{
        //    List<string> list = await DataGetSet.GetCatalogsAsync();
        //    _catalogs = new ObservableCollection<string>(list);
        //    _catalogs.Insert(0, "Alle");
        //    ListBox lb = listboxCatalogs;
        //    lb.ItemsSource = _catalogs;
        //    lb.SelectedItem = AudioStates.Catalog;
        //    lb.ScrollIntoView(lb.SelectedItem);
        //}

        //private async Task FillArtistsAsync()
        //{
        //    List<string> list = await DataGetSet.GetArtistsAsync();
        //    _artists = new ObservableCollection<string>(list);
        //    _artists.Insert(0, "Alle");
        //    ListBox lb = listboxArtists;
        //    lb.ItemsSource = _artists;
        //    lb.SelectedItem = AudioStates.Artist;
        //    if (lb.SelectedItem == null)
        //    {
        //        AudioStates.Artist = "";
        //        lb.SelectedItem = "Alle";
        //    }
        //}

        //private async Task FillAlbumsAsync()
        //{
        //    List<string> list = await DataGetSet.GetAlbumsAsync();
        //    _albums = new ObservableCollection<string>(list);
        //    _albums.Insert(0, "Alle");
        //    ListBox lb = listboxAlbums;
        //    lb.ItemsSource = _albums;
        //    lb.SelectedItem = AudioStates.Album;
        //    if (lb.SelectedItem == null)
        //    {
        //        AudioStates.Artist = "";
        //        lb.SelectedItem = "Alle";
        //    }
        //}
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
            GetSetData.SetSetting("LastQuery", "");

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
            GetSetData.SetSetting("LastQuery", textboxQuery.Text);
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
            var rowlist = (vPlaylistSongModel)datagrid.SelectedItem;
            int songId = rowlist.ID;

            var menuitem = sender as MenuItem;
            int plidnew = (int)menuitem.Tag;

            try
            {
                GetSetData.PlaylistEntryMove(songId, _lastPlaylistID, plidnew);

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
            var rowlist = (vSongModel)datagrid.SelectedItem;
            int songId = rowlist.ID;

            var menuitem = sender as MenuItem;
            var playlistID = (int)menuitem.Tag;

            try
            {
                GetSetData.AddSongToPlaylist(songId, playlistID);
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

                //datagrid.SelectedIndex = lastIndex;
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
        #endregion

        #region datagrid contentmenu old

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
                    Debug.Print(songId.ToString());
                    var result = DataGetSet.DeleteSong(songId);
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
            var rowlist = (vPlaylistSongModel)datagrid.SelectedItem;
            int songId = rowlist.ID;

            try
            {
                GetSetData.RemoveEntryFromPlaylist(songId, _lastPlaylistID);
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
