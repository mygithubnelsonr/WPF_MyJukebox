using MyJukebox.BLL;
using MyJukebox.Common;
using MyJukebox.DAL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private ObservableCollection<string> _queries;
        private ObservableCollection<Playlist> _playlists;

        private bool _isLoaded = false;
        private bool _dataLoaded = false;
        private bool _userIsDraggingSlider;
        private bool _mediaPlayerIsPlaying = false;
        private int _lastTab = -1;
        private int _lastPlaylist = -1;
        private RandomH random = new RandomH();

        private DispatcherTimer timer;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            FillPlaylists();
            SettingsDb.Load();

            mediaPlayer.Volume = 0;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;

            AudioStates.Genre = SettingsDb.GetSetting("LastGenre", "").ToString();
            AudioStates.Catalog = SettingsDb.GetSetting("LastCatalog", "").ToString();
            AudioStates.Artist = SettingsDb.GetSetting("LastArtist", "").ToString();
            AudioStates.Album = SettingsDb.GetSetting("LastAlbum", "").ToString();

            _lastTab = Convert.ToInt32(SettingsDb.GetSetting("LastTab", "0"));
            _lastPlaylist = Convert.ToInt32(SettingsDb.GetSetting("LastPlaylist", "0"));
        }

        #region FormEvents
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FillListboxesAsync();
            FillQueryCombo();

            tabcontrol.SelectedIndex = _lastTab;
            listboxPlaylists.SelectedIndex = _lastPlaylist;

            if (tabcontrol.SelectedIndex == 0)
                FillDatagridByTabLogical();
            else
                FillDatagridByTabPlaylist(_lastPlaylist);

            if (datagrid.ItemsSource != null)
            {
                datagrid.SelectedIndex = 0;
                datagrid.ScrollIntoView(datagrid.SelectedItem);
                datagrid.Focus();
                _dataLoaded = true;
            }

            statusGenre.Text = AudioStates.Genre;
            statusCatalog.Text = AudioStates.Catalog;
            statusArtist.Text = AudioStates.Artist;
            statusAlbum.Text = AudioStates.Album;
            _isLoaded = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SettingsDb.LastGenre = AudioStates.Genre;
            SettingsDb.LastCatalog = AudioStates.Catalog;
            SettingsDb.LastArtist = AudioStates.Artist;
            SettingsDb.LastAlbum = AudioStates.Album;
            SettingsDb.LastTab = tabcontrol.SelectedIndex;
            SettingsDb.Save();
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
            FillDatagridByTabLogical();
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
            FillDatagridByTabLogical();
        }

        private void listboxArtists_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string artist = listboxArtists.SelectedItem.ToString();
            AudioStates.Artist = artist == "Alle" ? "" : artist;
            FillAlbumsAsync();
            statusArtist.Text = artist;
            FillDatagridByTabLogical();
        }

        private void listboxAlbums_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string album = listboxAlbums.SelectedItem.ToString();
            AudioStates.Album = album == "Alle" ? "" : album;
            statusAlbum.Text = album;
            FillDatagridByTabLogical();
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            SettingsDb.LastGenre = AudioStates.Genre;
            SettingsDb.LastCatalog = AudioStates.Catalog;
            SettingsDb.LastArtist = AudioStates.Artist;
            SettingsDb.LastAlbum = AudioStates.Album;
            SettingsDb.Save();
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

        #endregion

        #region Methods

        #endregion

        #region async Methods
        private async Task FillListboxesAsync()
        {
            await FillGenresAsync();
            await FillCatalogsAsync();
            await FillArtistsAsync();
            await FillAlbumsAsync();
            //FillPlaylists();
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
            lb.SelectedIndex = _lastPlaylist;
            lb.ScrollIntoView(lb.SelectedItem);
        }

        #endregion

        #region Datagrid
        private void datagrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //_mediaPlayerIsPlaying = false;
            string fullpath = "";

            statusProgress.Text = @"00:00";
            statusDuration.Text = @"00:00";
            this.Title = "MyJukebox";

            if (_dataLoaded == false)
                return;

            if (datagrid.SelectedItem == null)
            {
                datagrid.SelectedIndex = 1;
                datagrid.CurrentItem = 1;
            }

            TabItem tab = tabcontrol.SelectedItem as TabItem;

            if (tab.Header.ToString() == "Audio")
            {
                var rowlist = (vSong)datagrid.SelectedItem;
                fullpath = $"{rowlist.Pfad}\\{rowlist.FileName}";
                this.Title = $"{rowlist.Artist} - {rowlist.Titel}";
            }

            if (tab.Header.ToString() == "Playlist")
            {
                var rowlist = (vPlaylistSong)datagrid.SelectedItem;
                if (rowlist != null)
                {
                    fullpath = $"{rowlist.Pfad}\\{rowlist.FileName}";
                    this.Title = $"{rowlist.Artist} - {rowlist.Titel}";
                }
            }

            mediaPlayer.Source = new Uri(fullpath);
        }

        private void FillDatagridByTabLogical()
        {
            _dataLoaded = false;
            List<vSong> results = DataGetSet.GetTablogicalResults();
            datagrid.ItemsSource = results;
            _dataLoaded = true;
            random.InitRandomNumbers(datagrid.Items.Count - 1);
        }
        #endregion

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            bool result = DataGetSet.PlaylistEntryMove(2260, 2, 12);
            if (result == false)
                MessageBox.Show("Move failed!", "Move Playlist Entry");
        }

        #region Slider Events
        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = sliderVolume.Value;
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
            textboxQuery.Text = "";
            TabItem tab = tabcontrol.SelectedItem as TabItem;
            if (tab.Header.ToString() == "Audio")
                FillDatagridByTabLogical();
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

            foreach (var item in comboboxStoredQueries.Items)
            {
                if (item.ToString().ToUpper() == query.ToUpper())
                    itemExist = true;
            }
            if (itemExist == false)
            {
                SettingsDb.QueryList.Add(query);
                _queries.Add(query);
            }
        }

        public void FillQueryCombo()
        {
            List<string> list = SettingsDb.QueryList;
            list.Insert(0, "");

            _queries = new ObservableCollection<string>(list);
            comboboxStoredQueries.ItemsSource = _queries;
        }

        private void FillDatagridByQuery()
        {
            _dataLoaded = false;
            if (textboxQuery.Text != "")
            {
                List<vSong> results = DataGetSet.GetQueryResult(textboxQuery.Text);

                random.InitRandomNumbers(datagrid.Items.Count - 1);

                if (results.Count == 0)
                {
                    datagrid.ItemsSource = null;
                }
                else
                {
                    datagrid.ItemsSource = results;
                    datagrid.SelectedIndex = 0;
                    datagrid.ScrollIntoView(datagrid.SelectedItem);
                    datagrid.Focus();
                }
            }
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
            timer.Stop();
            mediaPlayer.Stop();
            textboxQuery.Text = (string)comboboxStoredQueries.SelectedItem;
            FillDatagridByQuery();
        }

        private void comboboxStoredQueries_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                textboxQuery.Text = (string)comboboxStoredQueries.Text;
                FillDatagridByQuery();
            }
        }

        private void buttonQueryDelete_Click(object sender, RoutedEventArgs e)
        {
            SettingsDb.QueryList.Remove((string)comboboxStoredQueries.SelectedItem);
            _queries.Remove((string)comboboxStoredQueries.SelectedItem);
        }
        #endregion

        #region MediaPlayer Elements

        private void Play_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (mediaPlayer != null) && (mediaPlayer.Source != null);
        }

        private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            timer.Start();
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
            timer.Stop();
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _mediaPlayerIsPlaying;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mediaPlayer.Stop();
            _mediaPlayerIsPlaying = false;
            timer.Stop();
        }

        //buttonPlay.Command?.Execute(Button.CommandProperty);
        private void mediaplayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            _mediaPlayerIsPlaying = false;

            if (Playback_Shuffle.IsChecked == true)
            {
                var nextRow = random.GetNextNumber;
                datagrid.SelectedIndex = nextRow;
            }
            else if (Playback_Loop.IsChecked == true)
            {
                mediaPlayer.Position = TimeSpan.FromSeconds(0);
            }
            else
            {
                var currentItem = datagrid.SelectedIndex;
                if (currentItem + 1 < datagrid.Items.Count)
                    datagrid.SelectedIndex = currentItem + 1;
            }
        }

        private void Playback_Shuffle_Click(object sender, RoutedEventArgs e)
        {
            Playback_Loop.IsChecked = false;
        }

        private void Playback_Loop_Click(object sender, RoutedEventArgs e)
        {
            Playback_Shuffle.IsChecked = false;
        }

        #endregion

        private void timer_Tick(object sender, EventArgs e)
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

        private void listboxPlaylists_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Playlist playlist = listboxPlaylists.SelectedItem as Playlist;
            SettingsDb.LastPlaylist = playlist.ID;
            FillDatagridByTabPlaylist(playlist.ID);
        }

        private void FillDatagridByTabPlaylist(int playlistID)
        {
            var results = DataGetSet.GetPlaylistEntries(playlistID);
            datagrid.ItemsSource = results;

            if (datagrid.Items.Count > 1)
                datagrid.SelectedIndex = 0;
        }

        private void tabcontrol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoaded == false)
                return;

            mediaPlayer.Stop();
            _mediaPlayerIsPlaying = false;

            // add menu items to existing menu
            ContextMenu contextmenu = (ContextMenu)this.FindResource("contextmenuDatagrid");
            MenuItem miSendto = (MenuItem)contextmenu.Items[0];
            miSendto.Items.Clear();

            TabItem tab = tabcontrol.SelectedItem as TabItem;

            if (tab.Header.ToString() == "Audio")
            {
                miSendto.Header = "Send to";
                foreach (var playlist in _playlists)
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Header = playlist.Name;
                    menuItem.Tag = playlist.ID;
                    menuItem.Click += new RoutedEventHandler(this.contextmenuDatagridSendtoPlaylist_Click);
                    miSendto.Items.Add(menuItem);
                }
            }

            if (tab.Header.ToString() == "Playlist")
            {
                miSendto.Header = "Move to";
                foreach (var playlist in _playlists)
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Header = playlist.Name;
                    menuItem.Tag = playlist.ID;
                    menuItem.Click += new RoutedEventHandler(this.contextmenuDatagridMovetoPlaylist_Click);
                    miSendto.Items.Add(menuItem);
                }
            }
        }

        private void contextmenuDatagridMovetoPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var rowlist = (vPlaylistSong)datagrid.SelectedItem;
            int songId = rowlist.ID;

            var menuitem = sender as MenuItem;
            int plidnew = (int)menuitem.Tag;

            Debug.Print($"contextmenuDatagridMovetoPlaylist_Click: ID={menuitem.Tag}, Heder={menuitem.Header}");

            bool result = DataGetSet.PlaylistEntryMove(songId, _lastPlaylist, plidnew);
            if (result == false)
                MessageBox.Show("Move failed!", "Move Playlist Entry");

            FillDatagridByTabPlaylist(_lastPlaylist);

        }

        private void contextmenuDatagridSendtoPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var menuitem = sender as MenuItem;
            Debug.Print($"contextmenuDatagridSendtoPlaylist_Click: ID={menuitem.Tag}, Heder={menuitem.Header}");
        }

        #region ContextmenuDatagrid
        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TabItem tab = tabcontrol.SelectedItem as TabItem;

            if (_lastTab == 0)   // Audio
            {
                var rowlist = (vSong)datagrid.SelectedItem;
                Process.Start("explorer.exe", rowlist.Pfad);
            }
            else   // Playlist
            {
                var rowlist = (vPlaylistSong)datagrid.SelectedItem;
                Process.Start("explorer.exe", rowlist.Pfad);
            }
        }
        #endregion

        #region datagrid contentmenu
        private void datagridMenuitemSendTo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void datagridMenuitemCopyCell_Click(object sender, RoutedEventArgs e)
        {

        }

        private void datagridMenuitemCopyLine_Click(object sender, RoutedEventArgs e)
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
        #endregion
    }
}
