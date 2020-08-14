using MyJukebox.Common;
using MyJukebox.DAL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MyJukebox.BLL
{
    public class DataGetSet
    {
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
                Debug.Print($"TruncateTableImportTest_Error: {ex.Message}");
                return false;
            }

        }

        public static List<string> GetQueryList()
        {
            List<string> queries = null;

            var context = new MyJukeboxEntities();

            queries = context.tQueries
                            .Select(q => q.Query)
                            .OrderBy(q => q).ToList();

            return queries;
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
        #endregion

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

        //public static int SaveRecord(List<MP3Record> mP3Records)
        //{
        //    int recordsImporteds = 0;

        //    foreach (MP3Record record in mP3Records)
        //    {
        //        recordsImporteds += SetRecord(record);
        //    }

        //    return recordsImporteds;
        //}

        //public static bool TruncateTestTables()
        //{
        //    try
        //    {
        //        int result = -1;
        //        var context = new MyJukeboxEntities();
        //        result = context.Database.ExecuteSqlCommand("truncate table [tst].[tSongs]");
        //        result = context.Database.ExecuteSqlCommand("truncate table [tst].[tFileInfos]");
        //        result = context.Database.ExecuteSqlCommand("truncate table [tst].[tInfos]");
        //        result = context.Database.ExecuteSqlCommand("truncate table [tst].[tMD5]");

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.Print($"TruncateTableImportTest_Error: {ex.Message}");
        //        return false;
        //    }
        //}

        //private static bool MD5Exist(string MD5, bool testmode = false)
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

        //private static int SetRecord(MP3Record mp3Record)
        //{
        //    int lastSongID = -1;

        //    if (MD5Exist(mp3Record.MD5, false) == false)
        //    {
        //        try
        //        {
        //            var context = new MyJukeboxEntities();
        //            // tsongs data
        //            var songs = new tSong();
        //            songs.Album = mp3Record.Album;
        //            songs.Artist = mp3Record.Artist;
        //            songs.Titel = mp3Record.Titel;
        //            songs.Pfad = mp3Record.Path;
        //            songs.FileName = mp3Record.FileName;
        //            songs.ID_Genre = mp3Record.Genre;
        //            songs.ID_Catalog = mp3Record.Catalog;
        //            songs.ID_Media = mp3Record.Media;
        //            context.tSongs.Add(songs);
        //            context.SaveChanges();

        //            lastSongID = GetLastID("tSongs");

        //            // tmd5 data
        //            var md5 = new tMD5();
        //            md5.MD5 = mp3Record.MD5;
        //            md5.ID_Song = lastSongID;
        //            context.tMD5.Add(md5);
        //            context.SaveChanges();

        //            // tfileinfo data
        //            var file = new tFileInfo();
        //            file.FileDate = mp3Record.FileDate;
        //            file.FileSize = mp3Record.FileSize;
        //            file.ImportDate = DateTime.Now;
        //            file.ID_Song = lastSongID;
        //            context.tFileInfos.Add(file);
        //            context.SaveChanges();

        //            // tinfos data
        //            var info = new tInfo();
        //            info.Sampler = mp3Record.IsSample;
        //            info.ID_Song = lastSongID;
        //            context.tInfos.Add(info);
        //            context.SaveChanges();

        //            return 1;
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.Print($"SetNewTestRecord_Error: {ex.Message}");
        //            return 0;
        //        }
        //    }
        //    else
        //    {
        //        return 0;
        //    }
        //}

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


    }
}
