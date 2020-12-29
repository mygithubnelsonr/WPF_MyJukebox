using MyJukebox.Common;
using MyJukebox.DAL;
using MyJukebox.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MyJukebox.BLL
{
    public class DataGetSet
    {
        public enum DataSourceEnum
        {
            Songs,
            Playlist,
            Query
        }

        public static DataSourceEnum Datasource { get; set; }

        #region GetData

        public static List<string> GetGenres()
        {
            List<string> genres = null;
            using (var context = new MyJukeboxEntities())
            {
                genres = context.tGenres.Select(g => g.Name).ToList();
                return genres;
            }
        }

        public static List<string> GetCatalogs()
        {
            List<string> catalogues = null;

            using (var context = new MyJukeboxEntities())
            {
                catalogues = context.tCatalogs.Select(c => c.Name).ToList();
                return catalogues;
            }
        }

        public static List<string> GetArtists()
        {
            List<string> artist = null;

            using (var context = new MyJukeboxEntities())
            {
                artist = context.vSongs
                                .Where(i => i.Genre == AudioStates.Genre &&
                                    i.Catalog == AudioStates.Catalog &&
                                    i.Album.Contains(AudioStates.Album))
                                .Select(i => i.Artist)
                                .Distinct().OrderBy(i => i).ToList();

                return artist;
            }
        }

        public static bool TruncateTableQueries()
        {
            try
            {
                var context = new MyJukeboxEntities();
                var result = context.Database.ExecuteSqlCommand("truncate table [tQueries]");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR:TruncateTable");
                return false;
            }

        }

        public static List<vSong> GetQueryResult(string queryText)
        {
            List<vSong> songs = null;

            try
            {
                string sql = Helpers.GetQueryString(queryText);

                var context = new MyJukeboxEntities();
                songs = context.vSongs
                          .SqlQuery(sql).ToList();

                return songs;
            }
            catch (Exception ex)
            {
                Debug.Print($"GetQueryResultAsync: {ex.Message}");
                return null;
            }
        }

        public static List<Playlist> GetPlaylists()
        {
            List<Playlist> playLists = new List<Playlist>();

            using (var context = new MyJukeboxEntities())
            {
                var playlists = context.tPlaylists
                                .OrderBy(p => p.Name)
                                .Select(p => p);

                foreach (var p in playlists)
                {
                    playLists.Add(new Playlist { ID = p.ID, Name = p.Name, Last = (bool)p.Last, Row = p.Row });
                }

                return playLists;
            }
        }

        public static List<vPlaylistSong> GetPlaylistEntries(int playlistID)
        {
            List<vPlaylistSong> songs = null;
            try
            {
                var context = new MyJukeboxEntities();
                songs = context.vPlaylistSongs
                    .Where(i => i.PLID == playlistID).ToList();

                return songs;
            }
            catch (Exception ex)
            {
                Debug.Print($"GetPlaylistEntries_Error: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Async Methods

        public static async Task<List<string>> GetGenresAsync()
        {
            List<string> genres = null;
            using (var context = new MyJukeboxEntities())
            {
                await Task.Run(() =>
                {
                    genres = context.tGenres.Select(g => g.Name).ToList();

                });
                return genres;
            }
        }

        public static async Task<List<string>> GetCatalogsAsync()
        {
            List<string> catalogues = null;

            using (var context = new MyJukeboxEntities())
            {
                await Task.Run(() =>
                {
                    catalogues = context.tCatalogs.Select(c => c.Name).ToList();
                });
                return catalogues;
            }
        }

        public static async Task<List<string>> GetArtistsAsync()
        {
            List<string> artist = null;

            using (var context = new MyJukeboxEntities())
            {
                await Task.Run(() =>
                {
                    artist = context.vSongs
                                    .Where(i => i.Genre == AudioStates.Genre &&
                                        i.Catalog == AudioStates.Catalog)
                                    .Select(i => i.Artist)
                                    .Distinct()
                                    .OrderBy(i => i)
                                    .ToList();
                });

                return artist;
            }
        }

        public static async Task<List<string>> GetAlbumsAsync()
        {
            List<string> albums = null;

            using (var context = new MyJukeboxEntities())
            {
                await Task.Run(() =>
                {
                    albums = context.vSongs
                                    .Where(a => a.Genre == AudioStates.Genre &&
                                        a.Catalog == AudioStates.Catalog &&
                                        a.Artist.Contains(AudioStates.Artist))
                                    .Select(a => a.Album)
                                    .Distinct().OrderBy(a => a).ToList();
                });

                return albums;
            }
        }

        public static async Task<List<Playlist>> GetPlaylistsAsync()
        {
            List<Playlist> playlists = new List<Playlist>();

            using (var context = new MyJukeboxEntities())
            {
                await Task.Run(() =>
                {
                    var result = context.tPlaylists
                                    .OrderBy(p => p.Name)
                                    .Select(p => p)
                                    .ToList();

                    foreach (var p in result)
                    {
                        playlists.Add(new Playlist { ID = p.ID, Name = p.Name, Last = (bool)p.Last, Row = p.Row });
                    }

                });

                return playlists;
            }
        }

        #endregion

        public static void SetSetting(string keyName, string keyValue)
        {
            using (var context = new MyJukeboxEntities())
            {
                try
                {
                    var settingExits = context.tSettings
                                                .Where(s => s.Name == keyName)
                                                .FirstOrDefault();

                    if (settingExits == null)
                    {
                        context.tSettings
                            .Add(new tSetting { Name = keyName, Value = keyValue });

                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
            }
        }

        #region CreateCatalog(string catalog)
        //public static int CreateCatalog(string catalog)
        //{
        //    int idDbo = -1;
        //    int idTst = -1;

        //    var context = new MyJukeboxEntities();

        //    #region create new catalog on [dbo]
        //    var catalogDboExist = context.tCatalogs
        //                            .Where(c => c.Name == catalog)
        //                            .FirstOrDefault();

        //    if (catalogDboExist == null)
        //    {
        //        context.tCatalogs
        //            .Add(new tCatalog { Name = catalog });
        //        context.SaveChanges();

        //        idDbo = GetLastID("tCatalogs");
        //    }
        //    #endregion

        //    #region create new catalog on [tst]
        //    var catalogTstExist = context.tCatalogs
        //                            .Where(c => c.Name == catalog)
        //                            .FirstOrDefault();

        //    if (catalogTstExist == null)
        //    {
        //        context.tCatalogs
        //            .Add(new tCatalog { Name = catalog });
        //        context.SaveChanges();

        //        idTst = GetLastID("tCatalogs");
        //    }
        //    #endregion

        //    if (idDbo == idTst)
        //        return idDbo;
        //    else
        //        return -1;
        //}
        #endregion

        #region CreateGenre(string genre)
        //public static int CreateGenre(string genre)
        //{
        //    int id = -1;

        //    var context = new MyJukeboxEntities();

        //    var result = context.tGenres
        //                            .Where(g => g.Name == genre)
        //                            .FirstOrDefault();

        //    if (result == null)
        //    {
        //        context.tGenres
        //            .Add(new tGenre { Name = genre });
        //        context.SaveChanges();

        //        id = GetLastID("tGenres");
        //    }

        //    return id;
        //}
        #endregion

        #region SaveRecord(List<MP3Record> mP3Records)
        //public static int
        //{
        //    int recordsImporteds = 0;

        //    foreach (MP3Record record in mP3Records)
        //    {
        //        recordsImporteds += SetRecord(record);
        //    }

        //    return recordsImporteds;
        //}
        #endregion

        #region MD5Exist(string MD5, bool testmode = false)
        //private static bool
        //{
        //    object result = null;

        //    var context = new MyJukeboxEntities();

        //    if (testmode == false)
        //    {
        //        result = context.tMD5
        //                        .Where(m => m.MD5 == MD5).FirstOrDefault();

        //    }

        //    if (result != null)
        //    {
        //        Debug.Print($"title allready exist! (MD5={result})");
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}
        #endregion

        public static string GetSongFieldValuesByID(int id)
        {
            using (var context = new MyJukeboxEntities())
            {
                var entity = context.Set<tSong>().Find(id);
                var entry = context.Entry(entity);
                var currentPropertyValues = entry.CurrentValues;

                List<Tuple<string, object>> list = currentPropertyValues.PropertyNames
                    .Select(name => Tuple.Create(name, currentPropertyValues[name]))
                    .ToList();

                string result = "";
                int count = 0;
                foreach (var song in list)
                {
                    if (count > 5)
                        break;
                    result += song.Item2.ToString() + ",";
                    count++;
                }
                return result.Substring(0, result.Length - 1);
            }
        }

        public static int GetLastID(string tableName)
        {
            int lastId = -1;

            try
            {
                var context = new MyJukeboxEntities();

                if (tableName == "tGenres")
                {
                    var result = context.tGenres
                                    .Select(n => n.ID).Count();

                    if (result != 0)
                        lastId = context.tGenres.Max(n => n.ID);
                }

                if (tableName == "tCatalogs")
                {
                    var result = context.tCatalogs
                                    .Select(n => n.ID).Count();

                    if (result != 0)
                        lastId = context.tCatalogs.Max(n => n.ID);
                }

                if (tableName == "tSongs")
                {
                    var result = context.tSongs
                                    .Select(n => n.ID).Count();

                    if (result != 0)
                        lastId = context.tSongs.Max(n => n.ID);
                }

                return lastId;
            }
            catch
            {
                return -1;
            }
        }

        public static async Task<List<vSong>> GetTablogicalResultsAsync()
        {
            List<vSong> songs = null;

            try
            {
                var context = new MyJukeboxEntities();
                await Task.Run(() =>
                {
                    songs = context.vSongs
                        .Where(s =>
                            (s.Genre.Contains(AudioStates.Genre)) &&
                            (s.Catalog.Contains(AudioStates.Catalog)) &&
                            (s.Album.Contains(AudioStates.Album)) &&
                            (s.Artist.Contains(AudioStates.Artist))
                            ).ToList();
                });

                return songs;

            }
            catch (Exception ex)
            {
                Debug.Print($"GetTablogicalResultsAsync: {ex.Message}");
                return null;
            }
        }

        public static List<vSong> GetTablogicalResults()
        {
            List<vSong> songs = null;

            try
            {
                var context = new MyJukeboxEntities();
                songs = context.vSongs
                    .Where(s =>
                        (s.Genre.Contains(AudioStates.Genre)) &&
                        (s.Catalog.Contains(AudioStates.Catalog)) &&
                        (s.Artist.Contains(AudioStates.Artist)) &&
                        (s.Album.Contains(AudioStates.Album))
                        )
                    .ToList();

                return songs;
            }
            catch (Exception ex)
            {
                Debug.Print($"GetTablogicalResultsAsync: {ex.Message}");
                return null;
            }
        }

        public static bool PlaylistEntryMove(int songId, int plold, int plnew)
        {
            try
            {
                var context = new MyJukeboxEntities();
                var plentry = context.tPLentries
                                    .Where(p => p.PLID == plold && p.SongID == songId)
                                    .FirstOrDefault();

                if (plentry == null)
                    throw new ExceptionPlaylistMove("Record not found!");

                plentry.PLID = plnew;
                plentry.Pos = 1;
                context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR: PlaylistEntryMove");
                return false;
            }
        }

        public static bool AddSongToPlaylist(int idSong, int idPlaylist)
        {
            try
            {
                var context = new MyJukeboxEntities();
                var playlist = context.tPlaylists
                                    .Where(p => p.ID == idPlaylist)
                                    .Select(p => p.ID).ToList();

                if (playlist == null)
                    throw new ExceptionPlaylistNotExist("Wrong playlist!");

                var entry = context.tPLentries
                    .Where(p => p.ID == idPlaylist && p.SongID == idSong)
                    .FirstOrDefault();

                if (entry != null)
                    throw new ExceptionPlaylistSongExist("Song is allready in this Playlist! playlist!");

                var playlistentry = new tPLentry()
                {
                    PLID = idPlaylist,
                    SongID = idSong,
                    Pos = 1
                };

                context.tPLentries.Add(playlistentry);
                context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR: AddSongToPlaylist");
                return false;
            }
        }

        public static bool RemoveSongFromPlaylist(int idSong, int idPlaylist)
        {
            try
            {
                var context = new MyJukeboxEntities();
                var playlist = context.tPlaylists
                                    .Where(p => p.ID == idPlaylist)
                                    .FirstOrDefault();

                if (playlist == null)
                {
                    throw new ExceptionPlaylistNotExist($"Playlist {idPlaylist} not found!");
                }

                var entry = context.tPLentries
                    .Where(p => p.PLID == idPlaylist && p.SongID == idSong)
                    .FirstOrDefault();

                if (entry == null)
                    throw new ExceptionPlaylistSongNotExist($"Song not exist in the Playlist '{playlist.Name}'");

                context.tPLentries.Remove(entry);
                context.SaveChanges();

                return true;
            }
            catch (ExceptionPlaylistNotExist ex)
            {
                MessageBox.Show(ex.Message, "ERROR: RemoveSongFromPlaylist");
                return false;
            }
            catch (ExceptionPlaylistSongNotExist ex)
            {
                MessageBox.Show(ex.Message, "ERROR: RemoveSongFromPlaylist");
                return false;
            }
        }

        public static List<string> QueryGetList()
        {
            List<string> queries = null;

            var context = new MyJukeboxEntities();

            queries = context.tQueries
                            .Select(q => q.Name)
                            .OrderBy(q => q)
                            .ToList();

            return queries;
        }

        public static bool QueryAdd(string query)
        {
            try
            {
                var context = new MyJukeboxEntities();

                var result = context.tQueries
                        .Where(q => q.Name == query)
                        .FirstOrDefault();

                if (result != null)
                {
                    MessageBox.Show("The Query allready exist!", "Warning");
                    return true;
                }

                var newQuery = new tQuery()
                { Name = query, Row = 0 };

                context.tQueries.Add(newQuery);
                context.SaveChanges();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool QueryRemove(string query)
        {
            try
            {
                var context = new MyJukeboxEntities();

                var result = context.tQueries
                        .Where(q => q.Name == query)
                        .FirstOrDefault();

                if (result == null)
                {
                    MessageBox.Show("The Query not exist!", "ERROR");
                    return true;
                }

                context.tQueries.Remove(result);
                context.SaveChanges();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool QuerySetRow(string query, int row)
        {
            try
            {
                using (var context = new MyJukeboxEntities())
                {
                    var result = context.tQueries
                            .Where(q => q.Name == query)
                            .FirstOrDefault();

                    if (result == null)
                    {
                        MessageBox.Show("The Query not exist!", "ERROR");
                        return true;
                    }

                    result.Row = row;
                    context.SaveChanges();

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool DeleteSong(int id)
        {
            try
            {
                var context = new MyJukeboxEntities();
                var songs = context.tSongs.First(s => s.ID == id);
                context.tSongs.Remove(songs);
                context.SaveChanges();
                return true;
            }
            catch (ExceptionDeleteSong ex)
            {
                MessageBox.Show(ex.Message, "ERROR: DeleteSong");
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR: DeleteSong");
                return false;
            }
        }

        public static bool SetAlbumLastRow(string albumname, int row)
        {
            try
            {
                var context = new MyJukeboxEntities();
                var album = context.tAlbums
                                .Where(a => a.Name == albumname)
                                .FirstOrDefault();
                // create new entry
                if (album == null)
                {
                    album = new tAlbum();
                    album.Name = albumname;
                    album.Row = row;
                    context.tAlbums.Add(album);
                }
                else
                    album.Row = row;

                context.SaveChanges();
                return true;

            }
            catch
            {
                return false;
            }
        }

        public static int GetAlbumLastRow(string albumname)
        {
            try
            {
                var context = new MyJukeboxEntities();
                var album = context.tAlbums
                                .Where(a => a.Name == albumname)
                                .FirstOrDefault();
                if (album == null)
                    return 0;
                else
                    return (int)album.Row;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return 0;
            }
        }

        public static bool SetPlaylistLastRow(int id, int row)
        {
            try
            {
                var context = new MyJukeboxEntities();
                var playlist = context.tPlaylists
                                .Where(p => p.ID == id)
                                .FirstOrDefault();

                if (playlist != null)
                {
                    playlist.Row = row;
                    context.SaveChanges();
                }

                return true;

            }
            catch
            {
                return false;
            }
        }

        public static int GetPlaylistLastRow(int id)
        {
            try
            {
                var context = new MyJukeboxEntities();
                var playlist = context.tPlaylists
                                .Where(p => p.ID == id)
                                .FirstOrDefault();
                if (playlist == null)
                    return 0;
                else
                    return (int)playlist.Row;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return 0;
            }
        }

        public static bool SetQueryLastRow(string name, int row)
        {
            try
            {
                var context = new MyJukeboxEntities();
                var query = context.tQueries
                                .Where(q => q.Name == name)
                                .FirstOrDefault();

                if (query == null)
                    return true;

                query.Row = row;
                context.SaveChanges();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static int GetQueryLastRow(int index)
        {
            try
            {
                var context = new MyJukeboxEntities();
                var query = context.tQueries
                                .Where(q => q.ID == index)
                                .FirstOrDefault();
                if (query == null)
                    return 0;
                else
                    return query.Row == null ? 0 : (int)query.Row;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return 0;
            }
        }

        public static int GetQueryLastRow(string name)
        {
            try
            {
                var context = new MyJukeboxEntities();
                var query = context.tQueries
                                .Where(q => q.Name == name)
                                .FirstOrDefault();
                if (query == null)
                    return 0;
                else
                    return query.Row == null ? 0 : (int)query.Row;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return 0;
            }
        }

    }
}

