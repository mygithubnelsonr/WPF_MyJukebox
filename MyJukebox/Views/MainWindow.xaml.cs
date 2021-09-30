using MyJukeboxWMPDapper.Common;
using MyJukeboxWMPDapper.DataAccess;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MyJukeboxWMPDapper
{
    /// <summary>
    /// Version History:
    /// v2.2.2.0  12.05.2021  MyJukebox:  Button Shuffle - on click select first random number
    /// v2.2.3.0  13.05.2021  GetSetData: Implement DeleteSong ([spMyJukebox_DeleteSong])
    ///                       Fixed Playlist Playnext Song
    /// v2.2.4.0  15.05.2021  Refactoring: vPlaylistModel not longer needed. Clean code.
    /// v2.2.4.1  04.06.2021  Bugfix: repeat song after pause and then play
    /// v2.2.5.0  06.06.2021  Refactoring: Seperate Statusbar for Audio, Playlist and Query
    /// v2.2.5.1  12.06.2021  datagrid: make selected row visible (center if possible) 
    /// v2.2.5.2  17.06.2021  Refactoring: Implement saveing catalog lastrow per album
    /// v2.2.5.3  18.06.2021  Implement: catalog LastArtist
    /// v2.2.5.4  21.06.2021  Implement: show title in formheader
    /// v2.2.5.5  22.06.2021  implement: Autosized Listboxes
    /// v2.2.5.6  13.08.2021  implement: Play Pause when Spacebar is pressed
    /// v2.2.5.7  24.08.2021  implement: continue playing when Spacebar is pressed again (toggle)
    /// v2.2.5.8  26.08.2021  Bugfix: Stop and Message when filetype is '.m3u'
    /// v2.2.5.9  02.09.2021  Implement: Copy Celltext to Clipboard
    /// v2.2.5.10 07.09.2021  Implement: Open Browser with URL
    /// v2.2.5.11 13.09.2021  Bugfix: Set lastrow on textblockRowSelected when query loaded
    /// v2.2.5.12 17.09.2021  Implement: Add Slider Tooltip volume in percentage
    /// v2.2.6.1  30.09.2021  Implement: Combobox/Item Style
    /// </summary>

    public partial class MainWindow : Window
    {
        #region Fields
        private AxWMPLib.AxWindowsMediaPlayer wmp = null;
        private ObservableCollection<string> _queries;
        private ObservableCollection<PlaylistModel> _playlists;
        private List<string> _artistImageList;
        internal List<string> listTitles = new List<string>();

        private bool _isLoaded = false;
        private bool _dataLoaded = false;

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
        private string _cellvalue = "";

        private int _firstVisibleRow = 0;
        private int _lastVisibleRow = 0;

        private bool _isDraggingSlider;
        private string _fullpath = "";
        private bool _firstPlay = true;
        private bool _isPlaying = false;
        private bool _isPaused;
        private bool _isStoped;
        private bool _isLastDgRow;

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

            DynamicLoadStyles("Brown");

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
            AudioStates.Genre = "Alle";
            AudioStates.Catalog = "Alle";
            AudioStates.Album = "Alle";
            AudioStates.Artist = "Alle";

            AudioStates.Genre = GetSetData.GetSetting("LastGenre");
            _lastGenreID = Convert.ToInt32(GetSetData.GetSetting("LastGenreID"));

            _lastTab = Convert.ToInt32(GetSetData.GetSetting("LastTab"));
            _lastPlaylistID = Convert.ToInt32(GetSetData.GetSetting("LastPlaylistID"));
            _lastPlaylist = GetSetData.GetSetting("LastPlaylist");
            _lastQuery = GetSetData.GetSetting("LastQuery");

            this.Height = Convert.ToInt32(GetSetData.GetSetting("FormHeight"));
            this.Width = Convert.ToInt32(GetSetData.GetSetting("FormWidth"));
            this.Top = Convert.ToInt32(GetSetData.GetSetting("FormTop"));
            this.Left = Convert.ToInt32(GetSetData.GetSetting("FormLeft"));

            statusVersion.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            expanderLeftPanel.IsExpanded = Convert.ToBoolean(GetSetData.GetSetting("LeftPanel"));
            textboxQuery.Tag = GetSetData.GetSetting("PlaceHolder");
            sliderVolume.Value = 15;
            sliderVolume.ToolTip = $"{(int)sliderVolume.Value}%";

            #endregion
        }
        #endregion

        private enum Tab
        {
            Audio,
            Playlist
        }

        #region FormEvents
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string rootFolder = "";

            GetSetData.RefillTableAlbums();

            // initialize LbGenres then fill LbCatalogs then fill LbAlbums then fill LbAtrists
            InitializeListboxGenres();

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

                var record = (vSongModel)datagrid.SelectedItem;
                rootFolder = Helpers.GetShortPath(record.Path);
                _fullpath = $"{record.Path}\\{record.FileName}";
                _artist = record.Artist;

                _dataLoaded = true;

            }

            statusGenre.Text = AudioStates.Genre;
            statusCatalog.Text = AudioStates.Catalog;
            statusArtist.Text = AudioStates.Artist;
            statusAlbum.Text = AudioStates.Album;

            ImageFlipperLoad(rootFolder + "\\" + AudioStates.Genre, _isLoaded);
            wmp.settings.volume = (int)sliderVolume.Value;
            _isLoaded = true;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (_isPlaying)
                    Playback_Pause();
                else if (_isPaused)
                    Playback_Play();
            }
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

        private void menuThemeBlue_Click(object sender, RoutedEventArgs e)
        {
            DynamicLoadStyles("Blue");
        }

        private void menuThemeBrown_Click(object sender, RoutedEventArgs e)
        {
            DynamicLoadStyles("Brown");
        }

        private void menuToolsTest1_Click(object sender, RoutedEventArgs e)
        {
            //ScrollToCenter();
        }

        private void menuToolsTest2_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuDatabaseCheckPath_Click(object sender, RoutedEventArgs e)
        {
            Helpers.CheckSongPathExist();
        }

        private void touglebuttonSpeaker_Click(object sender, RoutedEventArgs e)
        {
            wmp.settings.mute = (bool)touglebuttonSpeaker.IsChecked;
        }

        private void tabcontrol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoaded == false)
                return;

            _lastTab = tabcontrol.SelectedIndex;
            GetSetData.SetSettingGeneral("LastTab", _lastTab.ToString());

            wmp.Ctlcontrols.stop();
            _isPlaying = false;

            DatagridContextmenuCreate();

            if (_lastTab == (int)Tab.Audio)
                FillDatagridByTabAudio();
            else
                FillDatagridByTabPlaylist();
        }

        private void expanderGenre_Expanded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            listboxGenres.Height = Double.NaN;
            rdgenre.Height = new GridLength(1, GridUnitType.Star);
        }

        private void expanderGenre_Collapsed(object sender, RoutedEventArgs e)
        {
            listboxGenres.Height = 0;
            rdgenre.Height = new GridLength(1, GridUnitType.Auto);
        }

        private void expanderCatalog_Expanded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            listboxCatalogs.Height = Double.NaN;
            rdcatalog.Height = new GridLength(1, GridUnitType.Star);
        }

        private void expanderCatalog_Collapsed(object sender, RoutedEventArgs e)
        {
            listboxCatalogs.Height = 0;
            rdcatalog.Height = new GridLength(1, GridUnitType.Auto);
        }

        private void expanderAlbum_Expanded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            listboxAlbums.Height = Double.NaN;
            rdalbum.Height = new GridLength(1, GridUnitType.Star);
        }

        private void expanderAlbum_Collapsed(object sender, RoutedEventArgs e)
        {
            listboxAlbums.Height = 0;
            rdalbum.Height = new GridLength(1, GridUnitType.Auto);
        }

        private void expanderArtist_Expanded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            listboxArtists.Height = Double.NaN;
            rdartist.Height = new GridLength(1, GridUnitType.Star);
        }

        private void expanderArtist_Collapsed(object sender, RoutedEventArgs e)
        {
            listboxArtists.Height = 0;
            rdartist.Height = new GridLength(1, GridUnitType.Auto);
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

            statusGenre.Text = AudioStates.Genre;

            FillListboxCatalogs();
            FillDatagridByTabAudio();
        }

        private void listboxCatalogs_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = (CatalogModel)listboxCatalogs.SelectedItem;

            if (item != null)
            {
                AudioStates.Catalog = item.Name;
                _lastCatalogID = item.ID;

                GetSetData.SetGenreCatalog(_lastGenreID, item.Name, item.ID);

                CatalogSetting catalog = GetSetData.GetCatalogSettings(_lastCatalogID, _lastAlbumID);
                if (catalog == null)
                {
                    AudioStates.Album = "Alle";
                    _lastAlbumID = 0;
                    _lastRow = 0;
                    GetSetData.SetCatalogSettings(AudioStates.Catalog, _lastCatalogID, AudioStates.Album, _lastAlbumID, _lastRow);
                }
                else
                {
                    AudioStates.Album = catalog.Album;
                    _lastAlbumID = catalog.ID_Album;
                    _lastRow = catalog.Row;
                }

                FillListboxAlbums();
            };

            if (listboxAlbums.Items.Count > 0)
            {
                listboxAlbums.SelectedIndex = 0;
                listboxAlbums.SelectedItem = 0;
            }

            statusCatalog.Text = AudioStates.Catalog;
            FillDatagridByTabAudio();
        }

        private void listboxAlbums_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = (AlbumModel)listboxAlbums.SelectedItem;

            AudioStates.Album = item.Name;
            _lastAlbumID = item.ID;

            GetSetData.SetCatalogAlbum(_lastCatalogID, AudioStates.Album, _lastAlbumID);


            CatalogSetting catalog = GetSetData.GetCatalogSettings(_lastCatalogID, _lastAlbumID);
            if (catalog == null)
            {
                _lastRow = 0;
                GetSetData.SetCatalogSettings(AudioStates.Catalog, _lastCatalogID, AudioStates.Album, _lastAlbumID, _lastRow);
            }
            else
            {
                AudioStates.Album = catalog.Album;
                _lastAlbumID = catalog.ID_Album;
                _lastRow = catalog.Row;
            }

            statusAlbum.Text = AudioStates.Album;

            FillListboxArtists();
            FillDatagridByTabAudio();
        }

        private void listboxArtists_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AudioStates.Artist = listboxArtists.SelectedItem.ToString();

            GetSetData.SetCatalogArtist(_lastCatalogID, AudioStates.Artist);

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
            wmp = formsHost.Child as AxWMPLib.AxWindowsMediaPlayer;
            wmp.uiMode = "none";
            wmp.settings.autoStart = false;
            wmp.enableContextMenu = false;
            wmp.AllowDrop = true;
            wmp.ErrorEvent += new EventHandler(player_ErrorEvent);
            wmp.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(player_PlayStateChange);
            wmp.ClickEvent += new AxWMPLib._WMPOCXEvents_ClickEventHandler(player_ClickEvent);
        }

        void player_ErrorEvent(object sender, EventArgs e)
        {
            MessageBox.Show("An error occured while trying to play the video.");
        }

        void player_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            //Debug.Print($"PlayStateChange: newState={WMPStatesText.Playstate(e.newState)}");

            if (e.newState == (int)WMPLib.WMPPlayState.wmppsMediaEnded)
            {
                //mediaplayer_MediaEnded();
            }

            if (e.newState == (int)WMPLib.WMPPlayState.wmppsStopped)
            {
                timerDuration.Stop();
                if (_isStoped == false)
                {
                    Dispatcher.BeginInvoke(new Action(() => Playback_Next()));
                    Dispatcher.BeginInvoke(new Action(() => Play_Executed(this, null)));
                }
            }

            if (e.newState == (int)WMPLib.WMPPlayState.wmppsPaused)
                _isPaused = true;

            if (e.newState == (int)WMPLib.WMPPlayState.wmppsPlaying)
            {
                _isPlaying = true;
                sliderPosition.Maximum = (int)wmp.currentMedia.duration;
                progressBarPosition.Maximum = sliderPosition.Maximum = wmp.currentMedia.duration;
                sliderPosition.TickFrequency = sliderPosition.Maximum / 10;
                statusDuration.Text = TimeSpan.FromSeconds(sliderPosition.Maximum).ToString(@"mm\:ss");
            }
        }

        void player_ClickEvent(object sender, AxWMPLib._WMPOCXEvents_ClickEvent e)
        {

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
                datagrid.Columns[9].Visibility = Visibility.Collapsed;

                CatalogSetting catalog = GetSetData.GetCatalogSettings(_lastCatalogID, _lastAlbumID);
                _lastRow = catalog.Row;

                if (datagrid.Items.Count - 1 < _lastRow)
                    _lastRow = 0;


                _dataLoaded = true;

                if (datagrid.Items.Count > 0)
                {
                    if (datagrid.Items.Count % 2 == 0)
                        datagrid.Background = new SolidColorBrush(Color.FromRgb(210, 234, 247));
                    else
                        datagrid.Background = new SolidColorBrush(Color.FromRgb(185, 223, 247));

                    datagrid.SelectedIndex = _lastRow;
                    datagrid.SelectedItem = _lastRow;
                    datagrid.ScrollIntoView(datagrid.SelectedItem);

                    var row = datagrid.Items.IndexOf(datagrid.SelectedItem);
                    textblockRowSelected.Text = (row + 1).ToString();

                    random.InitRandomNumbers(datagrid.Items.Count - 1);
                }
                SetStatusbar("audio");
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
                datagrid.Columns[9].Visibility = Visibility.Collapsed;
                _dataLoaded = true;

                _lastRow = GetSetData.GetPlaylistLastRow(_lastPlaylistID);

                if (datagrid.Items.Count > 0)
                {
                    datagrid.SelectedIndex = _lastRow;
                    datagrid.SelectedItem = _lastRow;
                    datagrid.ScrollIntoView(datagrid.SelectedItem);
                    random.InitRandomNumbers(datagrid.Items.Count - 1);
                }
                else
                {
                    datagrid.ItemsSource = null;
                }
                PlaylistModel playlist = listboxPlaylists.SelectedItem as PlaylistModel;

                textblockPlaylist.Text = playlist.Name;
                SetStatusbar("playlist");
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
                var results = GetSetData.GetQueryResult(textboxQuery.Text);
                if (results.Count > 0)
                {
                    GetSetData.Datasource = GetSetData.DataSourceEnum.Query;
                    datagrid.ItemsSource = results;
                    datagrid.Columns[9].Visibility = Visibility.Collapsed;

                    _lastRow = GetSetData.GetQueryLastRow((string)comboboxStoredQueries.SelectedItem);
                    datagrid.SelectedIndex = _lastRow;
                    datagrid.SelectedItem = _lastRow;
                    datagrid.ScrollIntoView(datagrid.SelectedItem);

                    var row = datagrid.Items.IndexOf(datagrid.SelectedItem);
                    textblockRowSelected.Text = (row + 1).ToString();

                    random.InitRandomNumbers(datagrid.Items.Count - 1);
                }
                SetStatusbar("query");
            }
            else
                QueryClear();

            _dataLoaded = true;
        }

        private void SetStatusbar(string statusbar)
        {
            sbplaylist.Visibility = Visibility.Hidden;
            sbquery.Visibility = Visibility.Hidden;
            sbaudio.Visibility = Visibility.Hidden;

            if (statusbar == "audio")
                sbaudio.Visibility = Visibility.Visible;
            if (statusbar == "playlist")
                sbplaylist.Visibility = Visibility.Visible;
            if (statusbar == "query")
                sbquery.Visibility = Visibility.Visible;
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

        private void InitializeListboxGenres()
        {
            List<GenreModel> genres = GetSetData.GetGenres();
            genres.Insert(0, new GenreModel { ID = 0, Name = "Alle" });

            listboxGenres.ItemsSource = genres;
            listboxGenres.SelectedIndex = _lastGenreID;
            listboxGenres.SelectedItem = _lastGenreID;
            listboxGenres.ScrollIntoView(listboxGenres.SelectedItem);
            listboxGenres.Focus();

            AudioStates.Catalog = GetSetData.GetLastCatalog(AudioStates.Genre);
            _lastCatalogID = GetSetData.GetLastCatalogID(AudioStates.Genre);

            FillListboxCatalogs();

            CatalogSetting catalog = GetSetData.GetCatalogSettings(_lastCatalogID, _lastAlbumID);
            if (catalog == null)
            {
                AudioStates.Album = "Alle";
                _lastAlbumID = 0;
                _lastRow = 0;
            }
            else
            {
                AudioStates.Album = catalog.Album;
                _lastAlbumID = catalog.ID_Album;
                _lastRow = catalog.Row;
            }
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

            AudioStates.Album = GetSetData.GetLastAlbum(AudioStates.Catalog);
            _lastAlbumID = GetSetData.GetLastAlbumID(AudioStates.Catalog);

            CatalogSetting catalog = GetSetData.GetCatalogSettings(_lastCatalogID, _lastAlbumID);

            if (catalog == null)
            {
                AudioStates.Album = "Alle";
                _lastAlbumID = 0;
                _lastRow = 0;
            }
            else
            {
                AudioStates.Album = catalog.Album;
                _lastAlbumID = catalog.ID_Album;
                _lastRow = catalog.Row;
            }

            statusCatalog.Text = AudioStates.Catalog;
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

            statusAlbum.Text = AudioStates.Album;

            FillListboxArtists();
        }

        private void FillListboxArtists()
        {
            List<string> artists = GetSetData.GetArtists(AudioStates.Genre, AudioStates.Catalog, AudioStates.Album);

            if (artists.Count > 1)
                artists.Insert(0, "Alle");

            var artist = GetSetData.GetLastArtist(AudioStates.Catalog);
            listboxArtists.ItemsSource = artists;

            AudioStates.Artist = artist == null ? "Alle" : artist;
            int index = GetArtistIndex(AudioStates.Artist);

            listboxArtists.SelectedIndex = index;
            listboxArtists.SelectedItem = index;
            listboxArtists.ScrollIntoView(listboxArtists.SelectedItem);
            listboxArtists.Focus();

            statusArtist.Text = AudioStates.Artist;

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

        private int GetNextIndex()
        {
            int newIndex = -1;
            int currentIndex = datagrid.SelectedIndex + 1;

            if (currentIndex < datagrid.Items.Count)
            {
                datagrid.SelectedIndex = currentIndex;
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

        private void ScrollToCenter()
        {
            if (datagrid.Items.Count > 0)
            {
                var border = VisualTreeHelper.GetChild(datagrid, 0) as Decorator;
                if (border != null)
                {
                    var scroll = border.Child as ScrollViewer;
                    if (scroll != null)
                    {
                        int index = datagrid.SelectedIndex;
                        int offset = (_lastVisibleRow - _firstVisibleRow) / 2;
                        if (index > _lastVisibleRow - 4)
                        {
                            scroll.ScrollToVerticalOffset(_lastVisibleRow - offset + 4);
                        }
                    }
                }
            }
        }

        private void DynamicLoadStyles(string text)
        {
            string fileName;

            if (text == "None")
            {
                // Clear any previous dictionaries loaded
                Resources.MergedDictionaries.Clear();
            }
            else
            {
                fileName = Environment.CurrentDirectory + @"\ResourceDictionarys\" + text + ".xaml";

                if (File.Exists(fileName))
                {
                    using (FileStream fs = new FileStream(fileName, FileMode.Open))
                    {
                        // Read in ResourceDictionary File
                        ResourceDictionary dic = (ResourceDictionary)XamlReader.Load(fs);
                        // Clear any previous dictionaries loaded
                        Resources.MergedDictionaries.Clear();
                        // Add in newly loaded Resource Dictionary
                        Resources.MergedDictionaries.Add(dic);
                    }
                }
                else
                    MessageBox.Show("File: " + text +
                       " does not exist. Please re-enter the name.");
            }
        }

        #endregion

        #region Datagrid
        private void datagrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            statusProgress.Text = @"00:00";
            statusDuration.Text = @"00:00";
            this.Title = "MyJukeboxWMPDapper";

            if (_dataLoaded == false || datagrid.Items.Count == 0)
                return;

            _isLastDgRow = false;
            var row = datagrid.Items.IndexOf(datagrid.SelectedItem);
            textblockRowSelected.Text = (row + 1).ToString();

            if (datagrid.SelectedItem == null)
            {
                datagrid.SelectedIndex = _lastRow;
                datagrid.CurrentItem = _lastRow;
            }

            var record = (vSongModel)datagrid.SelectedItem;

            _lastID = record.ID;
            _artist = record.Artist;
            _fullpath = $"{record.Path}\\{record.FileName}";
            this.Title = textblockCurrentSong.Text = $"{record.Artist} - {record.Title}";

            _lastRow = datagrid.SelectedIndex;

            if (GetSetData.Datasource == GetSetData.DataSourceEnum.Songs)
            {
                SetAlbumRow(AudioStates.Album, _lastRow);
                GetSetData.SetCatalogSettings(AudioStates.Catalog, _lastCatalogID, AudioStates.Album, _lastAlbumID, _lastRow);
            }

            if (GetSetData.Datasource == GetSetData.DataSourceEnum.Query)
                SetQueryRow(textboxQuery.Text, _lastRow);

            if (GetSetData.Datasource == GetSetData.DataSourceEnum.Playlist)
                SetPlaylistRow(_lastPlaylist, _lastRow);

            var root = Helpers.GetShortPath(_fullpath);
            ImageFlipperLoad(root + "\\" + AudioStates.Genre, _isLoaded);

            if (!_firstPlay)
                Playback_Play();
        }

        private void datagrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            _firstVisibleRow = (int)e.VerticalOffset;
            _lastVisibleRow = (int)e.VerticalOffset + (int)e.ViewportHeight;
        }

        #endregion

        #region Slider Events
        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_dataLoaded)
            {
                wmp.settings.volume = (int)sliderVolume.Value;
                sliderVolume.ToolTip = $"{(int)sliderVolume.Value}%";
            }
        }
        private void sliderVolume_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var vol = (e.Delta > 0) ? 5 : -5;
            sliderVolume.Value += vol;
            wmp.settings.volume += (int)vol;
        }

        private void sliderPosition_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _isDraggingSlider = true;
        }

        private void sliderPosition_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _isDraggingSlider = false;
            wmp.Ctlcontrols.currentPosition = sliderPosition.Value;
        }
        private void sliderPosition_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var part = sliderPosition.Maximum / 20;
            var pos = (e.Delta > 0) ? part : -part;

            sliderPosition.Value += pos;
            wmp.Ctlcontrols.currentPosition = sliderPosition.Value;
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
            AddQueryToComboBox(Helpers.ToTitleCase(textboxQuery.Text));
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
            //mediaPlayer.Stop();
            wmp.Ctlcontrols.stop();
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
            e.CanExecute = !String.IsNullOrEmpty(_fullpath);
        }

        private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Print("Play_Executed");
            _firstPlay = false;
            _isStoped = false;

            if (_artistImageList == null || _artistImageList.Count == 0)
            {
                Uri uri = new Uri("/Images/ShadowMen.gif", UriKind.Relative);
                BitmapImage image = new BitmapImage(uri);
                imageArtist.Source = image;
                imageArtist.Stretch = System.Windows.Media.Stretch.Uniform;
            }
            Playback_Play();
            _isPaused = false;
        }

        private void Pause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _isPlaying;
        }

        private void Pause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Playback_Pause();
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _isPlaying;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _isStoped = true;
            _firstPlay = true;
            Playback_Stop();
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
                var song = (vSongModel)datagrid.SelectedItem;
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
            int index = random.GetFirstNumber;
            datagrid.SelectedIndex = index;
            datagrid.SelectedItem = index;
            datagrid.ScrollIntoView(datagrid.SelectedItem);
        }

        #endregion

        #region Playback Methods
        private void Playback_Play()
        {
            Debug.Print("Playback_Play");

            if (_isLastDgRow)
                return;

            int m3u = _fullpath.IndexOf(".m3u");
            if (m3u > -1)
            {
                Playback_Stop();
                MessageBox.Show("Can not play m3u file!", "FILE ERROR");
                return;
            }

            if (_isPaused == false)
            {
                wmp.URL = _fullpath;
                sliderPosition.Value = 0;
            }

            wmp.Ctlcontrols.play();

            sliderPosition.TickFrequency = sliderPosition.Maximum / 10;
            progressBarPosition.Maximum = sliderPosition.Maximum;
            statusDuration.Text = TimeSpan.FromSeconds(sliderPosition.Maximum).ToString(@"mm\:ss");
            timerDuration.Start();
            timerFlipImage.Start();
        }

        private void Playback_Next()
        {
            Debug.Print("Playback_Next");

            int nextindex = 0;

            if ((bool)Playback_Loop.IsChecked)
                return;

            if ((bool)Playback_Shuffle.IsChecked)
            {
                nextindex = random.GetNextNumber;
            }
            else
                nextindex = GetNextIndex();

            if (nextindex < datagrid.Items.Count)
            {
                datagrid.SelectedIndex = nextindex;
                datagrid.SelectedItem = nextindex;
                ScrollToCenter();

                _fullpath = GetSongPath();
            }
            else
                _isLastDgRow = true;
        }

        private void Playback_Pause()
        {
            if (_isLoaded)
            {
                wmp.Ctlcontrols.pause();
                _isPaused = true;
                _isPlaying = false;
                timerDuration.Stop();
                timerFlipImage.Stop();
            }
        }

        private void Playback_Stop()
        {
            _isPlaying = false;
            _isPaused = false;
            _isStoped = true;
            if (_isLoaded)
            {
                timerDuration.Stop();
                timerFlipImage.Stop();
                wmp.Ctlcontrols.stop();
            }
        }

        private string GetSongPath()
        {
            string fullpath = "";

            var record = (vSongModel)datagrid.SelectedItem;
            fullpath = $"{record.Path}\\{record.FileName}";

            return fullpath;
        }

        ScrollViewer GetScrollViewer()
        {
            if (VisualTreeHelper.GetChildrenCount(this) == 0)
                return null;
            var x = VisualTreeHelper.GetChild(this, 0);
            if (x == null)
                return null;
            Debug.Print(x.ToString());
            if (VisualTreeHelper.GetChildrenCount(x) == 0)
                return null;
            return VisualTreeHelper.GetChild(x, 0) as ScrollViewer;
        }

        #endregion

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
                    var rowlist = (vSongModel)item;
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
                var rowlist = (vSongModel)datagrid.SelectedItem;
                Process.Start(rowlist.Path);

            }
            catch { }
        }

        private void datagrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(datagrid);
            TextBlock info = (TextBlock)datagrid.InputHitTest(p);
            _cellvalue = info.Text;
            //Debug.Print(_cellvalue);
        }

        private void datagridMenuitemCopyCell_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.Clear();
            Clipboard.SetText(_cellvalue);
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
                    var rowlist = (vSongModel)item;
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
            if ((wmp.URL != null) && (_isPlaying == true) && (_isDraggingSlider == false))
            {
                sliderPosition.Value = wmp.Ctlcontrols.currentPosition;
                statusProgress.Text = TimeSpan.FromSeconds(sliderPosition.Value).ToString(@"mm\:ss");
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

        private void datagridMenuitemOpenBrowser_Click(object sender, RoutedEventArgs e)
        {
            string token = _cellvalue.Replace(" ", "+");
            string url = "https://www.google.de/search?q=" + token;
            System.Diagnostics.Process.Start(url);
        }

        private void sliderPosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            string tooltip = $"{(int)sliderPosition.Value}%";
            Debug.Print(tooltip);
            sliderPosition.ToolTip = tooltip;
        }


    }
}
