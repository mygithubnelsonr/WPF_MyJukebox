using Dapper;
using MyJukeboxWMPDapper.Common;
using MyJukeboxWMPDapper.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace MyJukeboxWMPDapper.DataAccess
{
    public class GetSetData
    {
        private static string _connectionstring = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

        public enum DataSourceEnum
        {
            Songs,
            Playlist,
            Query
        }

        public static DataSourceEnum Datasource { get; set; }

        #region SongModel
        public static List<GenreModel> GetGenres()
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                string sql = "dbo.spMyJukebox_GenresGetall";
                var result = conn.Query<GenreModel>(sql).ToList();
                return result;
            }
        }

        public static List<CatalogModel> GetCatalogs(int genreId)
        {
            if (genreId == -1) return null;

            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                var p = new DynamicParameters();
                p.Add("@ID_Genre", genreId);

                string sql = "dbo.spMyJukebox_CatalogsByGenreID";
                var result = conn.Query<CatalogModel>(sql, p, commandType: CommandType.StoredProcedure).ToList();
                return result;
            }
        }

        public static List<string> GetArtists(string genre, string catalog, string album)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                var p = new DynamicParameters();
                p.Add("@Genre", genre == "Alle" ? "%" : genre);
                p.Add("@Catalog", catalog == "Alle" ? "%" : catalog);
                p.Add("@Album", album == "Alle" ? "%" : album);

                string sql = "dbo.spMyJukebox_GetArtistsByGenreCatalog";

                var result = conn.Query<string>(sql, p, commandType: CommandType.StoredProcedure).ToList();
                return result;
            }
        }

        public static List<AlbumModel> GetAlbums(int genreId, int catalogId)
        {
            if (genreId == -1 || catalogId == -1)
                return null;

            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                var p = new DynamicParameters();
                p.Add("@ID_Genre", genreId);
                p.Add("@ID_Catalog", catalogId);

                string sql = "dbo.spMyJukebox_GetAlbums";
                var albums = conn.Query<AlbumModel>(sql, p, commandType: CommandType.StoredProcedure).ToList();
                return albums;
            }
        }

        public static string GetLastCatalog(string genre)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                string sql = $"select * from tGenres where name = '{genre}'";
                var gen = conn.QuerySingle<GenreModel>(sql);
                string catalog = gen.LastCatalog;
                return catalog;
            }
        }

        public static int GetLastCatalogID(string genre)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                string sql = $"select * from tGenres where name = '{genre}'";
                var gen = conn.QuerySingle<GenreModel>(sql);
                int catalogid = gen.LastCatalogID;
                return catalogid;
            }
        }

        public static string GetLastAlbum(string catalog)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                string sql = $"select * from tCatalogs where name = '{catalog}'";
                var cat = conn.QuerySingle<CatalogModel>(sql);
                return cat.LastAlbum;
            }
        }

        public static int GetLastAlbumID(string catalog)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                string sql = $"select * from tCatalogs where name = '{catalog}'";
                var cat = conn.QuerySingle<CatalogModel>(sql);
                return cat.LastAlbumID;
            }
        }



        public static string GetLastArtist(string catalog)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                string sql = $"select * from tCatalogs where name = '{catalog}'";
                var cat = conn.QuerySingle<CatalogModel>(sql);
                return cat.LastArtist;
            }
        }


        public static List<AlbumModel> GetAlbumsByGenreID(int genreId)
        {
            if (genreId == -1)
                return null;

            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                var p = new DynamicParameters();
                p.Add("@ID_Genre", genreId);

                string sql = "dbo.spMyJukebox_GetAlbumsByGenreID";
                var albums = conn.Query<AlbumModel>(sql, p, commandType: CommandType.StoredProcedure).ToList();
                return albums;
            }
        }

        public static List<vSongModel> GetTitles(int genreID, int catalogID, string album, string artist)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                var p = new DynamicParameters();
                p.Add("@ID_Genre", genreID);
                p.Add("@ID_Catalog", catalogID);
                p.Add("@Artist", artist == "Alle" ? "%" : artist);
                p.Add("@Album", album == "Alle" ? "%" : album);

                string sql = "dbo.spMyJukebox_GetTitles";
                var result = conn.Query<vSongModel>(sql, p, commandType: CommandType.StoredProcedure).ToList();
                return result;
            }
        }

        public static List<vSongModel> GetQueryResult(string queryText)
        {
            List<vSongModel> songs;
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = GetQueryString(queryText);
                    songs = conn.Query<vSongModel>(sql).ToList();
                    return songs;
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"GetQueryResultAsync: {ex.Message}");
                return null;
            }
        }

        public static int GetCatalogID(string catalog)
        {
            if (String.IsNullOrEmpty(catalog) || catalog == "Alle")
                return 0;

            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                var p = new DynamicParameters();
                p.Add("@Name", catalog);
                p.Add("@ID", -1, DbType.Int32, ParameterDirection.Output);

                string sql = "[dbo].[spMyJukebox_GetCatalogIDByName]";
                var result = conn.Query(sql, p, commandType: CommandType.StoredProcedure);

                int catalogId = p.Get<int>("@ID");
                return catalogId;
            }
        }

        public static CatalogSetting GetCatalogSettings(int catalogid, int albumid)
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = $"select * from tSettingsCatalogs where ID_Catalog = {catalogid} and ID_Album = {albumid}";
                    var settings = conn.QueryFirstOrDefault<CatalogSetting>(sql);

                    if (settings == null)
                        return null;
                    else
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return null;
            }
        }

        //public static async void RefillTableAlbums()
        public static void RefillTableAlbums()
        {
            //using (IDbConnection conn = new SqlConnection(_connectionstring))
            //{
            //    try
            //    {
            //        string sql = "[dbo].[spMyJukebox_FillAlbums]";
            //        await conn.ExecuteAsync(sql, commandType: CommandType.StoredProcedure);
            //    }
            //    catch (Exception)
            //    {
            //        throw;
            //    }
            //}
        }

        public static int GetAlbumLastRow(string name)
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = $"select * from tSettingsAlbums where name = '{name}'";
                    var settings = conn.QueryFirstOrDefault(sql);

                    if (settings == null)
                        return 0;
                    else
                        return settings.Row;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return 0;
            }
        }

        public static bool SetSettingAlbumLastRow(string name, int row)
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = $"select * from tSettingsAlbums where name = '{name}'";
                    var settingExits = conn.QueryFirstOrDefault(sql);

                    if (settingExits == null)
                    {
                        sql = $"insert into tSettingsAlbums (Name, Row) values ('{name}', '{row}')";

                        var result = conn.Execute(sql);
                    }
                    else
                    {
                        sql = $"update tSettingsAlbums set row = '{row}' " +
                                $"where name = '{name}'";

                        var result = conn.Execute(sql);
                    }

                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SetCatalogSettings(string catalog, int idcatalog, string album, int idalbum, int row)
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = $"select * from tSettingsCatalogs where ID_Catalog = {idcatalog} and ID_Album = {idalbum}";
                    var settingExits = conn.QueryFirstOrDefault(sql);

                    if (settingExits == null)
                    {
                        string albumtemp = album.Replace("'", "''");

                        sql = $"insert into tSettingsCatalogs (Catalog, ID_Catalog, Album, ID_Album, Row) values ('{catalog}',{idcatalog},'{albumtemp}',{idalbum}, 0)";
                        var result = conn.Execute(sql);
                    }
                    else
                    {
                        sql = $"update tSettingsCatalogs set row = {row}" +
                                $"where ID_Catalog = {idcatalog} and ID_Album = {idalbum}";

                        var result = conn.Execute(sql);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return false;
            }
        }

        public static bool SetGenreCatalog(int genreID, string catalog, int catalogid)
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = $"update tGenres set LastCatalog = '{catalog}', LastCatalogID = {catalogid} where ID = {genreID}";
                    var result = conn.Execute(sql);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return false;
            }
        }

        public static bool SetCatalogAlbum(int catlogid, string album, int albumid)
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string albumtemp = album.Replace("'", "''");
                    string sql = $"update tCatalogs set LastAlbum = '{albumtemp}', LastAlbumID = {albumid} where ID = {catlogid}";
                    var result = conn.Execute(sql);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return false;
            }
        }

        public static bool SetCatalogArtist(int catlogid, string artist)
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = $"update tCatalogs set LastArtist = '{artist}' where ID = {catlogid}";
                    var result = conn.Execute(sql);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return false;
            }
        }

        public static string GetSongFieldValuesByID(int id)
        {
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                string sql = $"select * from vsongs where id = {id}";
                var songs = conn.Query(sql).ToList();
                var result = String.Join(",", songs);
                return result;
            }
        }

        internal static bool DeleteSong(int songId)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    var p = new DynamicParameters();
                    p.Add("@SONGID", songId);

                    string sql = $"[dbo].[spMyJukebox_DeleteSong]";

                    var retval = conn.Execute(sql, p, commandType: CommandType.StoredProcedure);

                    return true;
                }
                catch (ExceptionPlaylistSongNotExist ex)
                {
                    throw;
                }
            }
        }

        #endregion

        #region Query
        public static List<string> QueryGetList()
        {
            //List<string> queries = null;

            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                string sql = $"select name from tSettingsQueries order by name";
                var result = conn.Query<string>(sql).ToList();
                return result;
            }
        }

        public static string GetQueryString(string queryText)
        {
            queryText = queryText.Replace("'", "''");
            return $"select * from vsongs where charindex('{queryText}', lower( concat([path],[filename]))) > 0";
        }

        public static bool QueryAdd(string query)
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = "";
                    sql = $"select name from tSettingsQueries where name = '{query}'";
                    var found = conn.QueryFirstOrDefault(sql);

                    if (found != null)
                        return true;

                    sql = $"insert into tSettingsQueries (name, row) values ('{query}', 0)";
                    var result = conn.Execute(sql);
                    return true;
                }
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
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = "";
                    sql = $"select name from tSettingsQueries where name = '{query}'";
                    var found = conn.QueryFirstOrDefault(sql);

                    if (found == null)
                        return true;

                    sql = $"delete from tSettingsQueries where name = '{query}'";
                    var result = conn.Execute(sql);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        //public static bool QuerySetRow(string query, int row)
        //{
        //    try
        //    {
        //        using (var context = new MyJukeboxEntities())
        //        {
        //            var result = context.tQueries
        //                    .Where(q => q.Name == query)
        //                    .FirstOrDefault();

        //            if (result == null)
        //            {
        //                MessageBox.Show("The Query not exist!", "ERROR");
        //                return true;
        //            }

        //            result.Row = row;
        //            context.SaveChanges();

        //            return true;
        //        }
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        public static int GetQueryLastRow(string name)
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = $"select * from tSettingsQueries where name = '{name}'";
                    var settings = conn.QueryFirstOrDefault(sql);

                    if (settings == null)
                        return 0;
                    else
                        return settings.Row;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return 0;
            }
        }

        internal static bool SetQueryLastRow(string name, int row)
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = $"select * from tSettingsQueries where name = '{name}'";
                    var settingExits = conn.QueryFirstOrDefault(sql);

                    if (settingExits == null)
                    {
                        sql = $@"insert into tSettingsQueries (Name, Row)
                                values ('{name}', {row}, true)";

                        var result = conn.Execute(sql);
                    }
                    else
                    {
                        sql = $"update tSettingsQueries set row = '{row}' " +
                                $"where name = '{name}'";

                        var result = conn.Execute(sql);
                    }

                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Playlist
        public static List<PlaylistModel> GetAllPlaylists()
        {
            //List<PlaylistModel> playLists = new List<PlaylistModel>();

            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                string sql = $"select * from tSettingsPlaylists";
                var playLists = conn.Query<PlaylistModel>(sql).ToList();

                return playLists;
            }
        }

        public static PlaylistModel GetPlaylist(int id)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                string sql = $"select * from tSettingsPlaylists where id={id}";
                var playList = conn.QuerySingle<PlaylistModel>(sql);
                return playList;
            }
        }

        //public static int GetLastPlaylistID()   // no reference!
        //{
        //    string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");
        //    using (var conn = new SqlConnection(connection))
        //    {
        //        string sql = $"select max(id) from tSettingsPlaylists";
        //        int plid = conn.ExecuteScalar<int>(sql);

        //        return plid;
        //    }
        //}

        public static List<vSongModel> GetPlaylistEntries(int playlistID)
        {
            //List<vSongModel> songs;

            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    string sql = $"select * from vPlaylistSongs where plid = {playlistID}";
                    var songs = conn.Query<vSongModel>(sql).ToList();
                    return songs;
                }
                catch (Exception ex)
                {
                    Debug.Print($"GetPlaylistEntries_Error: {ex.Message}");
                    return null;
                }
            }
        }

        public static bool RemoveEntryFromPlaylist(int idSong, int idPlaylist)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    string sql = $"delete from tPLentries where plid={idPlaylist} and songid={idSong}";
                    var retval = conn.Execute(sql);

                    if (retval == 0)
                        throw new ExceptionPlaylistSongNotExist("Playlist or Song not found!");

                    return true;
                }
                catch (ExceptionPlaylistSongNotExist ex)
                {
                    throw;
                }
            }
        }

        public static bool PlaylistEntryMove(int songId, int plold, int plnew)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    string sql = $"update [dbo].[tPLentries] Set PLID = {plnew} where PLID = {plold} and SongID = {songId}";
                    var retval = conn.Execute(sql);

                    if (retval == 0)
                        throw new ExceptionPlaylistSongNotExist("Move Song to Playlist failed!");

                    return true;
                }
                catch
                {
                    throw;
                }
            }
        }

        public static bool AddSongToPlaylist(int idSong, int idPlaylist)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    string sql = $"insert into [dbo].[tPLentries] (PLID, SongID) values ({idPlaylist},{idSong})";
                    var retval = conn.Execute(sql);

                    if (retval == 0)
                        throw new ExceptionPlaylistSongNotExist("Add Song to Playlist failed!");

                    return true;
                }
                catch
                {
                    throw;
                }
            }
        }

        public static int GetPlaylistLastRow(int id)
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = $"select * from tSettingsPlaylists where id = {id}";
                    var settings = conn.QueryFirstOrDefault(sql);

                    if (settings == null)
                        return 0;
                    else
                        return settings.Row;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return 0;
            }
        }

        public static bool SetPlaylistLastRow(string name, int row)
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {
                    string sql = $"select * from tSettingsPlaylists where name = '{name}'";
                    var settingExits = conn.QueryFirstOrDefault(sql);

                    if (settingExits == null)
                    {
                        sql = $@"insert into tSettingsPlaylists (Name, Row)
                                values ('{name}', {row}, true)";

                        var result = conn.Execute(sql);
                    }
                    else
                    {
                        sql = $"update tSettingsPlaylists set row = '{row}' " +
                                $"where name = '{name}'";

                        var result = conn.Execute(sql);
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static int PlaylisAddNew(string playlistname)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    string sql = "";

                    sql = $"insert into [dbo].[tSettingsPlaylists] (Name,Last,Row) Values('{playlistname}','false',0)";
                    var retval = conn.Execute(sql);

                    sql = $"select max(id) from tSettingsPlaylists";
                    int plid = conn.ExecuteScalar<int>(sql);
                    return plid;
                }
                catch
                {
                    throw;
                }
            }
        }

        public static bool PlaylistRemove(int idPlaylist)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    string sql = "";

                    sql = $"delete from tPLentries where plid={idPlaylist}";
                    var retval = conn.Execute(sql);

                    sql = $"delete from dbo.tSettingsPlaylists where id={idPlaylist}";
                    var result = conn.Execute(sql);

                    if (result == 0)
                        throw new ExceptionPlaylistSongNotExist("Playlist or Song not found!");

                    return true;
                }
                catch (ExceptionPlaylistSongNotExist ex)
                {
                    throw;
                }
            }
        }

        public static bool PlaylistRename(int id, string playlistname)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    string sql = $"update dbo.tSettingsPlaylists set name = '{playlistname}' where id = {id}";
                    var retval = conn.Execute(sql);
                    return true;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        #endregion

        #region SettingsGeneral
        public static string GetSetting(string name)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                string sql = $"select * from tSettingsGeneral where name = '{name}'";
                var settings = conn.QueryFirstOrDefault(sql);

                if (settings == null || settings.Count == 0)
                    return "";
                else
                    return settings.Value;
            }
        }

        public static List<SettingsModel> LoadSettings()
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                string sql = $"select * from tSettingsGeneral order by name";
                var result = conn.Query<SettingsModel>(sql).ToList();
                return result;
            }
        }

        public static void SetSettingGeneral(string keyName, string keyValue)
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    string sql = $"select * from tSettingsGeneral where name = '{keyName}'";
                    var settingExits = conn.Query(sql).SingleOrDefault();

                    if (settingExits == null)
                    {
                        sql = $@"insert into tSettingsGeneral (Name, Value, Editable)
                                values ('{keyName}', '{keyValue}', 'true')";

                        var result = conn.Execute(sql);
                    }
                    else
                    {
                        sql = $"update tSettingsGeneral set Value = '{keyValue}' " +
                                $"where name = '{keyName}'";

                        var result = conn.Execute(sql);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
            }
        }

        public static List<string> GetSongPathList()
        {
            try
            {
                using (IDbConnection conn = new SqlConnection(_connectionstring))
                {

                    string sql = "";
                    string shortpath = "";

                    sql = "select top 1 path from tSongs";
                    string fullpath = conn.ExecuteScalar<string>(sql);

                    shortpath = Helpers.GetShortPath(fullpath);
                    sql = $"SELECT DISTINCT [Path] FROM tSongs WHERE [Path] like '{shortpath}\\%'";
                    var pathlist = conn.Query<string>(sql).ToList();
                    return pathlist;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return null;
            }
        }

        #endregion
    }
}
