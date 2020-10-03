﻿using MyJukebox.BLL;
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

// ToDo set lastrow on album and playlist
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

        private int _lastID = -1;
        private int _lastRow = -1;
        private int _lastTab = -1;
        private int _lastPlaylist = -1;
        private int _datasource = -1;
        private string _lastQuery = "";
        private bool _dataLoaded = false;
        private bool _isLoaded = false;
        private bool _userIsDraggingSlider;
        private bool _mediaPlayerIsPlaying = false;

        private RandomH random = new RandomH();

        private DispatcherTimer timer;

        #endregion

        #region CTOR
        public MainWindow()
        {
            InitializeComponent();

            mediaPlayer.Volume = 0;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;

            AudioStates.Genre = SettingsDb.Settings["LastGenre"];
            AudioStates.Catalog = SettingsDb.Settings["LastCatalog"];
            AudioStates.Artist = SettingsDb.Settings["LastArtist"];
            AudioStates.Album = SettingsDb.Settings["LastAlbum"];

            _lastRow = Convert.ToInt32(SettingsDb.Settings["LastRow"]);
            _lastTab = Convert.ToInt32(SettingsDb.Settings["LastTab"]);
            _lastPlaylist = Convert.ToInt32(SettingsDb.Settings["LastPlaylist"]);
            _lastQuery = SettingsDb.Settings["LastQuery"];
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
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FillListboxesAsync();
            FillQueryCombo();

            tabcontrol.SelectedIndex = _lastTab;

            var query = SettingsDb.Settings["LastQuery"];

            if (!String.IsNullOrEmpty(SettingsDb.Settings["LastQuery"]))
            {
                _lastRow = DataGetSet.GetQueryLastRow(SettingsDb.Settings["LastQuery"]);
                comboboxStoredQueries.Text = SettingsDb.Settings["LastQuery"];
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

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            //SaveSettings();
            SetPlaylistRow(_lastPlaylist, _lastRow);
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
            bool result = DataGetSet.SetQueryLastRow((string)comboboxStoredQueries.SelectedItem, _lastRow);
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
                SettingsDb.Save();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
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

            if (_datasource == (int)DataSource.Songs || _datasource == (int)DataSource.Query)
            {
                var rowlist = (vSong)datagrid.SelectedItem;
                _lastID = rowlist.ID;
                fullpath = $"{rowlist.Pfad}\\{rowlist.FileName}";
                this.Title = $"{rowlist.Artist} - {rowlist.Titel}";
            }

            if (_datasource == (int)DataSource.Playlist)
            {
                var rowlist = (vPlaylistSong)datagrid.SelectedItem;
                _lastID = rowlist.ID;
                if (rowlist != null)
                {
                    fullpath = $"{rowlist.Pfad}\\{rowlist.FileName}";
                    this.Title = $"{rowlist.Artist} - {rowlist.Titel}";
                }
            }

            _lastRow = datagrid.SelectedIndex;

            if (_datasource == (int)DataSource.Songs)
                SetAlbumRow(AudioStates.Album, _lastRow);

            if (_datasource == (int)DataSource.Query)
                SetQueryRow(textboxQuery.Text, _lastRow);

            if (_datasource == (int)DataSource.Playlist)
                SetPlaylistRow(_lastPlaylist, _lastRow);

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
            comboboxStoredQueries.Text = _lastQuery;

        }

        private void FillDatagridByQuery()
        {
            _dataLoaded = false;
            if (textboxQuery.Text != "")
            {
                List<vSong> results = DataGetSet.GetQueryResult(textboxQuery.Text);

                if (results != null)
                {
                    _datasource = (int)DataSource.Query;
                    datagrid.ItemsSource = results;
                    //_dataLoaded = true;
                    random.InitRandomNumbers(datagrid.Items.Count - 1);

                    _lastRow = DataGetSet.GetQueryLastRow((string)comboboxStoredQueries.SelectedItem);

                    if (datagrid.Items.Count > 1)
                        datagrid.SelectedIndex = _lastRow;
                }


                //if (results.Count == 0)
                //{
                //    datagrid.ItemsSource = null;
                //}
                //else
                //{
                //    datagrid.ItemsSource = results;
                //    datagrid.SelectedIndex = _lastRow;
                //    datagrid.ScrollIntoView(datagrid.SelectedItem);
                //    datagrid.Focus();
                //    _datasource = (int)DataSource.Songs;
                //}
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
            timer.Stop();
            mediaPlayer.Stop();
            textboxQuery.Text = (string)comboboxStoredQueries.SelectedItem;
            SettingsDb.Settings["LastQuery"] = textboxQuery.Text;
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
                _mediaPlayerIsPlaying = true;
            }
            else if (Playback_Loop.IsChecked == true)
            {
                mediaPlayer.Position = TimeSpan.FromSeconds(0);
                _mediaPlayerIsPlaying = true;
            }
            else
            {
                var currentItem = datagrid.SelectedIndex;
                if (currentItem + 1 < datagrid.Items.Count)
                    datagrid.SelectedIndex = currentItem + 1;
                _mediaPlayerIsPlaying = true;
            }
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
                _datasource = (int)DataSource.Songs;
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
                _datasource = (int)DataSource.Playlist;
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
            Debug.Print(mi.Header.ToString());
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
    }
}
