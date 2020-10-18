using MyJukebox.BLL;
using MyJukebox.Common;
using MyJukebox.DAL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MyJukebox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private ObservableCollection<string> _genres;
        private ObservableCollection<string> _catalogs;
        private ObservableCollection<string> _artists;
        private ObservableCollection<string> _albums;
        //private ObservableCollection<Query> _queries;
        private ObservableCollection<string> _queries;
        private ObservableCollection<Playlist> _playlists;
        private List<string> _artistImageList;

        private int _lastID = -1;
        private int _lastRow = -1;
        private int _lastTab = -1;
        private int _lastPlaylist = -1;
        private string _lastQuery = "";
        private string _artist = "";
        private bool _dataLoaded = false;
        private bool _isLoaded = false;
        private bool _userIsDraggingSlider;
        private bool _mediaPlayerIsPlaying = false;

        private RandomH random = new RandomH();
        private DispatcherTimer timerDuration;
        private DispatcherTimer timerFlipImage;

        #endregion

        #region CTOR
        public MainWindow()
        {
            InitializeComponent();

            mediaPlayer.Volume = 0;
            timerDuration = new DispatcherTimer();
            timerDuration.Interval = TimeSpan.FromSeconds(1);
            timerDuration.Tick += timerDuration_Tick;
            timerDuration.IsEnabled = false;

            timerFlipImage = new DispatcherTimer();
            timerFlipImage.Interval = TimeSpan.FromSeconds(10);
            timerFlipImage.Tick += timerFlipImage_Tick;
            timerFlipImage.IsEnabled = true;
            timerFlipImage.Stop();

            AudioStates.Genre = SettingsDb.Settings["LastGenre"];
            AudioStates.Catalog = SettingsDb.Settings["LastCatalog"];
            AudioStates.Artist = SettingsDb.Settings["LastArtist"];
            AudioStates.Album = SettingsDb.Settings["LastAlbum"];

            _lastRow = Convert.ToInt32(SettingsDb.Settings["LastRow"]);
            _lastTab = Convert.ToInt32(SettingsDb.Settings["LastTab"]);
            _lastPlaylist = Convert.ToInt32(SettingsDb.Settings["LastPlaylist"]);
            _lastQuery = SettingsDb.Settings["LastQuery"];

            this.Height = Convert.ToInt32(SettingsDb.Settings["FormHeight"]);
            this.Width = Convert.ToInt32(SettingsDb.Settings["FormWidth"]);

            expanderLeftPanel.IsExpanded = Convert.ToBoolean(SettingsDb.Settings["LeftPanel"]);

            textboxQuery.Tag = SettingsDb.Settings["PlaceHolder"];
            FillPlaylists();
        }
        #endregion

        public enum DataSource
        {
            Songs,
            Playlist,
            Query
        }

        #region FormEvents

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ImageFlipperLoad()
        {
            imageArtist.Tag = 0;

            var imageFlipper = ImageFlipper.Instanz();
            string rootPath = SettingsDb.Settings["RootImagePath"];
            string imagePath = SettingsDb.Settings["ImagePath"];
            string fullpath = Path.Combine(
                rootPath,
                AudioStates.Genre,
                imagePath);

            _artistImageList = imageFlipper.GetImagesFullNames(fullpath, _artist);

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Top = Convert.ToInt32(SettingsDb.Settings["FormTop"]);
            this.Left = Convert.ToInt32(SettingsDb.Settings["FormLeft"]);

            FillListboxesAsync();
            FillQueryCombo();

            tabcontrol.SelectedIndex = _lastTab;

            if (!String.IsNullOrEmpty(_lastQuery))
            {
                _lastRow = DataGetSet.GetQueryLastRow(_lastQuery);
                FillDatagridByQuery();
            }
            else
            {
                if (tabcontrol.SelectedIndex == 0)
                {
                    _lastRow = DataGetSet.GetAlbumLastRow(AudioStates.Album);
                    FillDatagridByTabAudio();
                }
                else
                {
                    _lastRow = DataGetSet.GetPlaylistLastRow(_lastPlaylist);
                    FillDatagridByTabPlaylist(_lastPlaylist);
                }
            }

            if (datagrid.ItemsSource != null)
            {
                datagrid.SelectedIndex = _lastRow;
                datagrid.SelectedItem = _lastRow;
                datagrid.ScrollIntoView(datagrid.SelectedItem);
                datagrid.Focus();

                //DataGetSet.Datasource == DataGetSet.DataSourceEnum.Songs
                if (DataGetSet.Datasource == DataGetSet.DataSourceEnum.Songs ||
                    DataGetSet.Datasource == DataGetSet.DataSourceEnum.Query)
                {
                    var rowlist = (vSong)datagrid.SelectedItem;
                    _artist = rowlist.Artist;
                }

                if (DataGetSet.Datasource == DataGetSet.DataSourceEnum.Playlist)
                {
                    var rowlist = (vPlaylistSong)datagrid.SelectedItem;
                    _artist = rowlist.Artist;
                }
                _dataLoaded = true;
            }

            statusGenre.Text = AudioStates.Genre;
            statusCatalog.Text = AudioStates.Catalog;
            statusArtist.Text = AudioStates.Artist;
            statusAlbum.Text = AudioStates.Album;
            sliderVolume.Value = Convert.ToDouble(SettingsDb.Settings["Volume"]);

            _isLoaded = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        private void Move_Window(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void listboxGenres_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string genre = listboxGenres.SelectedItem.ToString();
            AudioStates.Genre = genre == "Alle" ? "" : genre;
            FillCatalogsAsync();
            FillArtistsAsync();
            FillAlbumsAsync();
            statusGenre.Text = genre;
            FillDatagridByTabAudio();
        }

        private void listboxCatalogs_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string catalog = listboxCatalogs.SelectedItem.ToString();
            AudioStates.Catalog = catalog == "Alle" ? "" : catalog;

            FillArtistsAsync();
            string artist = listboxArtists.SelectedItem.ToString();
            AudioStates.Artist = artist == "Alle" ? "" : artist;

            FillAlbumsAsync();
            string album = listboxAlbums.SelectedItem.ToString();
            AudioStates.Album = album == "Alle" ? "" : album;
            statusAlbum.Text = album;
            FillDatagridByTabAudio();
        }

        private void listboxArtists_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string artist = listboxArtists.SelectedItem.ToString();
            AudioStates.Artist = artist == "Alle" ? "" : artist;
            FillAlbumsAsync();
            statusArtist.Text = artist;
            FillDatagridByTabAudio();
        }

        private void listboxAlbums_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string album = listboxAlbums.SelectedItem.ToString();
            AudioStates.Album = album == "Alle" ? "" : album;
            statusAlbum.Text = album;
            FillDatagridByTabAudio();
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            this.Close();
        }

        private void menuEditRecordLocation_Click(object sender, RoutedEventArgs e)
        {
            Window input = new Views.InputBox();
            input.Show();
            input = null;
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
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

        private void SetAlbumRow(string name, int row)
        {
            bool result = DataGetSet.SetAlbumLastRow(name, row);
        }

        private void SetPlaylistRow(int id, int row)
        {
            bool result = DataGetSet.SetPlaylistLastRow(id, row);
        }

        private void SetQueryRow(string name, int row)
        {
            bool result = DataGetSet.SetQueryLastRow(textboxQuery.Text, _lastRow);
        }

        #endregion

        #region Methods
        private void SaveSettings()
        {
            try
            {
                SettingsDb.Settings["LastGenre"] = AudioStates.Genre;
                SettingsDb.Settings["LastCatalog"] = AudioStates.Catalog;
                SettingsDb.Settings["LastArtist"] = AudioStates.Artist;
                SettingsDb.Settings["LastAlbum"] = AudioStates.Album;
                SettingsDb.Settings["LastTab"] = _lastTab.ToString();
                SettingsDb.Settings["Volume"] = Convert.ToString(mediaPlayer.Volume);
                SettingsDb.Settings["LastRow"] = Convert.ToString(_lastRow);
                SettingsDb.Settings["FormHeight"] = this.Height.ToString();
                SettingsDb.Settings["FormWidth"] = this.Width.ToString();
                SettingsDb.Settings["FormTop"] = this.Top.ToString();
                SettingsDb.Settings["FormLeft"] = this.Left.ToString();
                SettingsDb.Settings["LeftPanel"] = expanderLeftPanel.IsExpanded.ToString();
                SettingsDb.Save();
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

        #endregion

        #region async Methods
        private async Task FillListboxesAsync()
        {
            await FillGenresAsync();
            await FillCatalogsAsync();
            await FillArtistsAsync();
            await FillAlbumsAsync();
        }

        private async Task FillPlaylistsAsync()
        {
            List<Playlist> list = await DataGetSet.GetPlaylistsAsync();

            _playlists = new ObservableCollection<Playlist>(list);

            ListBox lb = listboxPlaylists;
            lb.ItemsSource = _playlists;
            lb.SelectedIndex = _lastPlaylist;
            lb.ScrollIntoView(lb.SelectedItem);
        }

        private async Task FillGenresAsync()
        {
            List<string> list = await DataGetSet.GetGenresAsync();

            _genres = new ObservableCollection<string>(list);
            _genres.Insert(0, "Alle");

            ListBox lb = listboxGenres;
            lb.ItemsSource = _genres;
            lb.SelectedItem = AudioStates.Genre;
            lb.ScrollIntoView(lb.SelectedItem);
        }

        private async Task FillCatalogsAsync()
        {
            List<string> list = await DataGetSet.GetCatalogsAsync();
            _catalogs = new ObservableCollection<string>(list);
            _catalogs.Insert(0, "Alle");
            ListBox lb = listboxCatalogs;
            lb.ItemsSource = _catalogs;
            lb.SelectedItem = AudioStates.Catalog;
            lb.ScrollIntoView(lb.SelectedItem);
        }

        private async Task FillArtistsAsync()
        {
            List<string> list = await DataGetSet.GetArtistsAsync();
            _artists = new ObservableCollection<string>(list);
            _artists.Insert(0, "Alle");
            ListBox lb = listboxArtists;
            lb.ItemsSource = _artists;
            lb.SelectedItem = AudioStates.Artist;
            if (lb.SelectedItem == null)
            {
                AudioStates.Artist = "";
                lb.SelectedItem = "Alle";
            }
        }

        private async Task FillAlbumsAsync()
        {
            List<string> list = await DataGetSet.GetAlbumsAsync();
            _albums = new ObservableCollection<string>(list);
            _albums.Insert(0, "Alle");
            ListBox lb = listboxAlbums;
            lb.ItemsSource = _albums;
            lb.SelectedItem = AudioStates.Album;
            if (lb.SelectedItem == null)
            {
                AudioStates.Artist = "";
                lb.SelectedItem = "Alle";
            }
        }

        private void FillPlaylists()
        {
            List<Playlist> list = DataGetSet.GetPlaylists();

            _playlists = new ObservableCollection<Playlist>(list);

            ListBox lb = listboxPlaylists;
            lb.ItemsSource = _playlists;
            lb.SelectedIndex = GetSelectedIndex(_lastPlaylist);
            lb.ScrollIntoView(lb.SelectedItem);
        }

        private int GetSelectedIndex(int plid)
        {
            int listindex = -1;

            foreach (Playlist pl in listboxPlaylists.Items)
            {
                listindex += 1;
                if (pl.ID == plid)
                    break;
            }

            return listindex;
        }

        #endregion

        #region Datagrid
        private void datagrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string fullpath = "";

            statusProgress.Text = @"00:00";
            statusDuration.Text = @"00:00";
            this.Title = "MyJukebox";

            if (_dataLoaded == false)
                return;

            if (datagrid.SelectedItem == null)
            {
                datagrid.SelectedIndex = _lastRow;
                datagrid.CurrentItem = _lastRow;
            }

            if (DataGetSet.Datasource == DataGetSet.DataSourceEnum.Songs ||
                DataGetSet.Datasource == DataGetSet.DataSourceEnum.Query)
            {
                var rowlist = (vSong)datagrid.SelectedItem;
                _lastID = rowlist.ID;
                _artist = rowlist.Artist;
                fullpath = $"{rowlist.Pfad}\\{rowlist.FileName}";
                this.Title = $"{rowlist.Artist} - {rowlist.Titel}";
            }

            if (DataGetSet.Datasource == DataGetSet.DataSourceEnum.Playlist)
            {
                var rowlist = (vPlaylistSong)datagrid.SelectedItem;
                _lastID = rowlist.ID;
                _artist = rowlist.Artist;
                if (rowlist != null)
                {
                    fullpath = $"{rowlist.Pfad}\\{rowlist.FileName}";
                    this.Title = $"{rowlist.Artist} - {rowlist.Titel}";
                }
            }

            _lastRow = datagrid.SelectedIndex;

            if (DataGetSet.Datasource == DataGetSet.DataSourceEnum.Songs)
                SetAlbumRow(AudioStates.Album, _lastRow);

            if (DataGetSet.Datasource == DataGetSet.DataSourceEnum.Query)
                SetQueryRow(textboxQuery.Text, _lastRow);

            if (DataGetSet.Datasource == DataGetSet.DataSourceEnum.Playlist)
                SetPlaylistRow(_lastPlaylist, _lastRow);

            ImageFlipperLoad();
            mediaPlayer.Source = new Uri(fullpath);
        }

        #endregion

        #region Slider Events
        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_dataLoaded)
            {
                mediaPlayer.Volume = sliderVolume.Value;
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
            SettingsDb.Settings["LastQuery"] = textboxQuery.Text;
        }

        private void QueryClear()
        {
            textboxQuery.Text = "";
            comboboxStoredQueries.Text = "";

            TabItem tab = tabcontrol.SelectedItem as TabItem;
            if (tab.Header.ToString() == "Audio")
                FillDatagridByTabAudio();
            else
                FillDatagridByTabPlaylist(_lastPlaylist);
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
            bool itemExist = false;

            // ToDo replace with LINQ
            foreach (var item in comboboxStoredQueries.Items)
            {
                if (item.ToString().ToUpper() == query.ToUpper())
                    itemExist = true;
            }

            if (itemExist == false)
            {
                //SettingsDb.QueryList.Add(query);

                bool result = DataGetSet.QueryAdd(query);
                if (result == true)
                    _queries.Add(query);
            }
        }

        public void FillQueryCombo()
        {
            //List<string> list = SettingsDb.QueryList;

            List<string> list = null;

            list = DataGetSet.QueryGetList();

            list.Insert(0, "");

            _queries = new ObservableCollection<string>(list);
            comboboxStoredQueries.ItemsSource = _queries;
            comboboxStoredQueries.Text = _lastQuery;
            textboxQuery.Text = _lastQuery;
        }

        private void FillDatagridByQuery()
        {
            _dataLoaded = false;
            if (textboxQuery.Text != "")
            {
                List<vSong> results = DataGetSet.GetQueryResult(textboxQuery.Text);

                if (results != null)
                {
                    DataGetSet.Datasource = DataGetSet.DataSourceEnum.Query;
                    datagrid.ItemsSource = results;
                    random.InitRandomNumbers(datagrid.Items.Count - 1);

                    _lastRow = DataGetSet.GetQueryLastRow((string)comboboxStoredQueries.SelectedItem);


                    if (datagrid.Items.Count > 1)
                        datagrid.SelectedIndex = _lastRow;
                }
            }
            else
                QueryClear();

            _dataLoaded = true;
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
            SettingsDb.Settings["LastQuery"] = textboxQuery.Text;
            FillDatagridByQuery();
        }

        private void comboboxStoredQueries_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Enter)
            //{
            //    textboxQuery.Text = (string)comboboxStoredQueries.Text;
            //    FillDatagridByQuery();
            //}
        }

        private void buttonQueryDelete_Click(object sender, RoutedEventArgs e)
        {
            string query = (string)comboboxStoredQueries.SelectedItem;

            bool result = DataGetSet.QueryRemove(query);

            if (result == true)
                _queries.Remove((string)comboboxStoredQueries.SelectedItem);
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
                var song = (vPlaylistSong)datagrid.SelectedItem;
                id = song.ID;
            }
            else
            {
                var song = (vSong)datagrid.SelectedItem;
                id = song.ID;
            }

            string songFields = DataGetSet.GetSongFieldValuesByID(id);

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
        }

        #endregion

        private void mediaplayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            _mediaPlayerIsPlaying = false;

            if (Playback_Shuffle.IsChecked == true)
            {
                var nextRow = random.GetNextNumber;
                datagrid.SelectedIndex = nextRow;
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

        private void SetArtistImage(string path)
        {
            Uri uri;
            BitmapImage image;

            uri = new Uri(path, UriKind.RelativeOrAbsolute);
            image = new BitmapImage(uri);
            imageArtist.Source = image;
            imageArtist.Stretch = System.Windows.Media.Stretch.Uniform;
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

        private void listboxPlaylists_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Playlist playlist = listboxPlaylists.SelectedItem as Playlist;
            SettingsDb.Settings["LastPlaylist"] = playlist.ID.ToString();
            _lastPlaylist = playlist.ID;
            FillDatagridByTabPlaylist(playlist.ID);
        }

        private void FillDatagridByTabAudio()
        {
            _dataLoaded = false;
            List<vSong> results = DataGetSet.GetTablogicalResults();
            if (results != null)
            {
                DataGetSet.Datasource = DataGetSet.DataSourceEnum.Songs;
                datagrid.ItemsSource = results;
                _dataLoaded = true;
                random.InitRandomNumbers(datagrid.Items.Count - 1);

                _lastRow = DataGetSet.GetAlbumLastRow(AudioStates.Album);

                if (datagrid.Items.Count > 1)
                    datagrid.SelectedIndex = _lastRow;
            }
        }

        private void FillDatagridByTabPlaylist(int playlistID)
        {
            _dataLoaded = false;
            var results = DataGetSet.GetPlaylistEntries(playlistID);
            if (results != null)
            {
                DataGetSet.Datasource = DataGetSet.DataSourceEnum.Playlist;
                datagrid.ItemsSource = results;
                _dataLoaded = true;

                random.InitRandomNumbers(datagrid.Items.Count - 1);

                _lastRow = DataGetSet.GetPlaylistLastRow(_lastPlaylist);

                if (datagrid.Items.Count > 1)
                    datagrid.SelectedIndex = _lastRow;
            }
        }

        private void tabcontrol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoaded == false)
                return;

            _lastTab = tabcontrol.SelectedIndex;

            mediaPlayer.Stop();
            _mediaPlayerIsPlaying = false;

            DatagridContextmenuCreate();
        }

        #region ContextmenuDatagrid

        private void DatagridContextmenuCreate()
        {
            TabItem tab = tabcontrol.SelectedItem as TabItem;

            if (tab.Header.ToString() == "Audio")
            {
                DatagridContextmenuCreateAudio();
                if (!String.IsNullOrEmpty(_lastQuery))
                    FillDatagridByQuery();
                else
                    FillDatagridByTabAudio();
            }

            if (tab.Header.ToString() == "Playlist")
            {
                DatagridContextmenuCreatePlaylist();
                FillDatagridByTabPlaylist(_lastPlaylist);
            }

            if (datagrid.Items.Count >= _lastRow)
                datagrid.SelectedIndex = _lastRow;
            else
                datagrid.SelectedIndex = 0;
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
            //Debug.Print(mi.Header.ToString());
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
            var rowlist = (vPlaylistSong)datagrid.SelectedItem;
            int songId = rowlist.ID;

            var menuitem = sender as MenuItem;
            int plidnew = (int)menuitem.Tag;
            //Debug.Print($"contextmenuDatagridMovetoPlaylist_Click: ID={menuitem.Tag}, Heder={menuitem.Header}");

            bool result = DataGetSet.PlaylistEntryMove(songId, _lastPlaylist, plidnew);
            if (result == false)
                MessageBox.Show("Move failed!", "Move Playlist Entry");

            FillDatagridByTabPlaylist(_lastPlaylist);
        }

        private void contextmenuDatagridSendtoPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var rowlist = (vSong)datagrid.SelectedItem;
            int songId = rowlist.ID;

            var menuitem = sender as MenuItem;
            var playlistID = (int)menuitem.Tag;
            //Debug.Print($"contextmenuDatagridSendtoPlaylist_Click: SongID={songId}, PlaylistID={menuitem.Tag}, Heder={menuitem.Header},");
            DataGetSet.AddSongToPlaylist(songId, playlistID);
        }

        private void datagridMenuitemOpenEditor_Click(object sender, RoutedEventArgs e)
        {
            int lastIndex = datagrid.SelectedIndex;

            try
            {
                Process.Start(SettingsDb.Settings["RecordEditorLocation"], _lastID.ToString());

                if (_lastTab == 0)
                    FillDatagridByTabAudio();
                else
                    FillDatagridByTabPlaylist(_lastPlaylist);

                datagrid.SelectedIndex = lastIndex;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR: Open Record Editor");
            }
        }

        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TabItem tab = tabcontrol.SelectedItem as TabItem;
            try
            {
                if (_lastTab == 0)   // Audio
                {
                    var rowlist = (vSong)datagrid.SelectedItem;
                    Process.Start(rowlist.Pfad);
                }
                else   // Playlist
                {
                    var rowlist = (vPlaylistSong)datagrid.SelectedItem;
                    Process.Start(rowlist.Pfad);
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
            //Debug.Print("contextmenuDatagridRemoveFromAudio_Click");

            var rowlist = (vSong)datagrid.SelectedItem;
            int songId = rowlist.ID;

            var result = DataGetSet.DeleteSong(songId);

            FillDatagridByTabAudio();
        }

        private void contextmenuDatagridRemoveFromPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var rowlist = (vPlaylistSong)datagrid.SelectedItem;
            int songId = rowlist.ID;

            var result = DataGetSet.RemoveSongFromPlaylist(songId, _lastPlaylist);

            FillDatagridByTabPlaylist(_lastPlaylist);
        }

        #endregion

    }
}
