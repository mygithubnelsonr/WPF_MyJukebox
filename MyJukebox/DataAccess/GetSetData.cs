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

        public static List<string> GetArtists(string genre, string catalog, string album)
        {
            using (var conn = new SqlConnection(_connectionstring))
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

        public static List<vSongModel> GetQueryResult(string queryText)
        {
            List<vSongModel> songs;
            try
            {
                using (var conn = new SqlConnection(_connectionstring))
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

        public static List<GenreModel> GetGenres()
        {
            using (var conn = new SqlConnection(_connectionstring))
            {
                string sql = "dbo.spMyJukebox_GenresGetall";
                var result = conn.Query<GenreModel>(sql).ToList();
                return result;
            }
        }

        //public static List<CatalogModel> GetCatalogs(string genre)
        //{
        //    if (String.IsNullOrEmpty(genre))
        //        return null;

        //    string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");
        //    using (var conn = new SqlConnection(connection))
        //    {
        //        var p = new DynamicParameters();
        //        p.Add("@Genre", genre);

        //        string sql = "dbo.spMyJukebox_CatalogsByGenre";
        //        var result = conn.Query<CatalogModel>(sql, p, commandType: CommandType.StoredProcedure).ToList();
        //        return result;
        //    }
        //}

        public static List<CatalogModel> GetCatalogs(int genreId)
        {
            if (genreId == -1) return null;

            using (var conn = new SqlConnection(_connectionstring))
            {
                var p = new DynamicParameters();
                p.Add("@ID_Genre", genreId);

                string sql = "dbo.spMyJukebox_CatalogsByGenreID";
                var result = conn.Query<CatalogModel>(sql, p, commandType: CommandType.StoredProcedure).ToList();
                return result;
            }
        }

        public static int GetCatalogID(string catalog)
        {
            if (String.IsNullOrEmpty(catalog) || catalog == "Alle")
                return 0;

            using (var conn = new SqlConnection(_connectionstring))
            {
                var p = new DynamicParameters();
                p.Add("@Name", catalog);
                p.Add("@ID", -1, DbType.Int32, ParameterDirection.Output);

                string sql = "sbMyJukebox_GetCatalogIDByName";
                var result = conn.Query(sql, p, commandType: CommandType.StoredProcedure);

                int catalogId = p.Get<int>("@ID");
                return catalogId;
            }
        }

        public static async void RefillTableAlbums()
        {
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    string sql = "[dbo].[spMyJukebox_FillAlbums]";
                    await conn.ExecuteAsync(sql, commandType: CommandType.StoredProcedure);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }


        public static List<AlbumModel> GetAlbums(int genreId, int catalogId)
        {
            if (genreId == -1 || catalogId == -1)
                return null;

            using (var conn = new SqlConnection(_connectionstring))
            {
                var p = new DynamicParameters();
                p.Add("@ID_Genre", genreId);
                p.Add("@ID_Catalog", catalogId);

                string sql = "dbo.spMyJukebox_GetAlbums";
                var albums = conn.Query<AlbumModel>(sql, p, commandType: CommandType.StoredProcedure).ToList();
                return albums;
            }
        }

        public static List<AlbumModel> GetAlbumsByGenreID(int genreId)
        {
            if (genreId == -1)
                return null;

            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");
            using (var conn = new SqlConnection(connection))
            {
                var p = new DynamicParameters();
                p.Add("@ID_Genre", genreId);

                string sql = "dbo.spMyJukebox_GetAlbumsByGenreID";
                var albums = conn.Query<AlbumModel>(sql, p, commandType: CommandType.StoredProcedure).ToList();
                return albums;
            }
        }

        public static int GetAlbumLastRow(string name)
        {
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            try
            {
                using (var conn = new SqlConnection(connection))
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
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            try
            {
                using (var conn = new SqlConnection(connection))
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

        public static bool SetGenreCatalog(int genreID, string catalog, int catalogid)
        {
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");
            try
            {
                using (var conn = new SqlConnection(connection))
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

        public static string GetSongFieldValuesByID(int id)
        {
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            using (var conn = new SqlConnection(connection))
            {
                string sql = $"select * from vsongs where id = {id}";
                var songs = conn.Query(sql).ToList();
                var result = String.Join(",", songs);
                return result;
            }
        }

        internal static object DeleteSong(int songId)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Query
        public static List<string> QueryGetList()
        {
            List<string> queries = null;

            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            using (var conn = new SqlConnection(connection))
            {
                string sql = $"select name from tSettingsQueries order by name";
                queries = conn.Query<string>(sql).ToList();
                return queries;
            }
        }

        public static string GetQueryString(string queryText)
        {
            queryText = queryText.Replace("'", "''");
            return $"select * from vsongs where charindex('{queryText}', lower( concat([path],[filename]))) > 0";
        }

        public static bool QueryAdd(string query)
        {
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            try
            {
                using (var conn = new SqlConnection(connection))
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
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");
            try
            {
                using (var conn = new SqlConnection(connection))
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

        public static bool QuerySetRow(string query, int row)
        {
            try
            {
                //using (var context = new MyJukeboxEntities())
                //{
                //    var result = context.tQueries
                //            .Where(q => q.Name == query)
                //            .FirstOrDefault();

                //    if (result == null)
                //    {
                //        MessageBox.Show("The Query not exist!", "ERROR");
                //        return true;
                //    }

                //    result.Row = row;
                //    context.SaveChanges();

                return true;
                //}
            }
            catch
            {
                return false;
            }
        }

        public static int GetQueryLastRow(string name)
        {
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            try
            {
                using (var conn = new SqlConnection(connection))
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
                using (var conn = new SqlConnection(_connectionstring))
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
            List<PlaylistModel> playLists = new List<PlaylistModel>();

            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");
            using (var conn = new SqlConnection(connection))
            {
                string sql = $"select * from tSettingsPlaylists";
                playLists = conn.Query<PlaylistModel>(sql).ToList();

                return playLists;
            }
        }

        public static PlaylistModel GetPlaylist(int id)
        {
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");
            using (IDbConnection conn = new SqlConnection(connection))
            {
                string sql = $"select * from tSettingsPlaylists where id={id}";
                var playList = conn.QuerySingle<PlaylistModel>(sql);
                return playList;
            }
        }

        public static int GetLastPlaylistID()   // no reference!
        {
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");
            using (var conn = new SqlConnection(connection))
            {
                string sql = $"select max(id) from tSettingsPlaylists";
                int plid = conn.ExecuteScalar<int>(sql);

                return plid;
            }
        }

        public static List<vPlaylistSongModel> GetPlaylistEntries(int playlistID)
        {
            List<vPlaylistSongModel> songs;
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            using (var conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    string sql = $"select * from vPlaylistSongs where plid = {playlistID}";
                    songs = conn.Query<vPlaylistSongModel>(sql).ToList();
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
            using (var conn = new SqlConnection(_connectionstring))
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
            using (var conn = new SqlConnection(_connectionstring))
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
            using (var conn = new SqlConnection(_connectionstring))
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
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            try
            {
                using (var conn = new SqlConnection(_connectionstring))
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
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            try
            {
                using (var conn = new SqlConnection(connection))
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
                string sql = "";
                try
                {
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
            string sql = "";
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    sql = $"delete from tPLentries where plid={idPlaylist}";
                    var retval = conn.Execute(sql);

                    // retval can be 0 if no songs in zhe playlist
                    //if (retval == 0)
                    //    throw new ExceptionPlaylistSongNotExist("Playlist or Song not found!");

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
            string sql = "";
            using (IDbConnection conn = new SqlConnection(_connectionstring))
            {
                try
                {
                    sql = $"update dbo.tSettingsPlaylists set name = '{playlistname}' where id = {id}";
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
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            using (var conn = new SqlConnection(connection))
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
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            using (var conn = new SqlConnection(connection))
            {
                string sql = $"select * from tSettingsGeneral order by name";
                var result = conn.Query<SettingsModel>(sql).ToList();
                return result;
            }
        }

        public static void SetSettingGeneral(string keyName, string keyValue)
        {
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            using (var conn = new SqlConnection(connection))
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
            string sql = "";
            string shortpath = "";
            string connection = ConnectionTools.GetConnectionString("MyJukeboxWMPDapper");

            try
            {
                using (var conn = new SqlConnection(connection))
                {
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
