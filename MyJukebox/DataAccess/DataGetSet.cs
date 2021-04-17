using MyJukeboxWMPDapper.Exceptions;
using System;
using System.Windows;

namespace MyJukeboxWMPDapper.DataAccess
{
    public class DataGetSet
    {

        #region GetData

        //public static List<Playlist> GetPlaylists()
        //{
        //    List<Playlist> playLists = new List<Playlist>();

        //    using (var context = new MyJukeboxEntities())
        //    {
        //        var playlists = context.tPlaylists
        //                        .OrderBy(p => p.Name)
        //                        .Select(p => p);

        //        foreach (var p in playlists)
        //        {
        //            playLists.Add(new Playlist { ID = p.ID, Name = p.Name, Last = (bool)p.Last, Row = p.Row });
        //        }

        //        return playLists;
        //    }
        //}


        #endregion

        #region Async Methods

        //public static async Task<List<Playlist>> GetPlaylistsAsync()
        //{
        //    List<Playlist> playlists = new List<Playlist>();

        //    using (var context = new MyJukeboxEntities())
        //    {
        //        await Task.Run(() =>
        //        {
        //            var result = context.tPlaylists
        //                            .OrderBy(p => p.Name)
        //                            .Select(p => p)
        //                            .ToList();

        //            foreach (var p in result)
        //            {
        //                playlists.Add(new Playlist { ID = p.ID, Name = p.Name, Last = (bool)p.Last, Row = p.Row });
        //            }

        //        });

        //        return playlists;
        //    }
        //}

        #endregion

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

        #region public static bool PlaylistEntryMove(int songId, int plold, int plnew)
        //{
        //    try
        //    {
        //        //var context = new MyJukeboxEntities();
        //        //var plentry = context.tPLentries
        //        //                    .Where(p => p.PLID == plold && p.SongID == songId)
        //        //                    .FirstOrDefault();

        //        //if (plentry == null)
        //        //    throw new ExceptionPlaylistMove("Record not found!");

        //        //plentry.PLID = plnew;
        //        //plentry.Pos = 1;
        //        //context.SaveChanges();

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message, "ERROR: PlaylistEntryMove");
        //        return false;
        //    }
        //}
        #endregion

        #region public static bool AddSongToPlaylist(int idSong, int idPlaylist)
        //{
        //    try
        //    {
        //        //var context = new MyJukeboxEntities();
        //        //var playlist = context.tPlaylists
        //        //                    .Where(p => p.ID == idPlaylist)
        //        //                    .Select(p => p.ID).ToList();

        //        //if (playlist == null)
        //        //    throw new ExceptionPlaylistNotExist("Wrong playlist!");

        //        //var entry = context.tPLentries
        //        //    .Where(p => p.ID == idPlaylist && p.SongID == idSong)
        //        //    .FirstOrDefault();

        //        //if (entry != null)
        //        //    throw new ExceptionPlaylistSongExist("Song is allready in this Playlist! playlist!");

        //        //var playlistentry = new tPLentry()
        //        //{
        //        //    PLID = idPlaylist,
        //        //    SongID = idSong,
        //        //    Pos = 1
        //        //};

        //        //context.tPLentries.Add(playlistentry);
        //        //context.SaveChanges();

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message, "ERROR: AddSongToPlaylist");
        //        return false;
        //    }
        //}
        #endregion

        #region public static bool RemoveSongFromPlaylist(int idSong, int idPlaylist)
        //{
        //    try
        //    {
        //        //var context = new MyJukeboxEntities();
        //        //var playlist = context.tPlaylists
        //        //                    .Where(p => p.ID == idPlaylist)
        //        //                    .FirstOrDefault();

        //        //if (playlist == null)
        //        //{
        //        //    throw new ExceptionPlaylistNotExist($"Playlist {idPlaylist} not found!");
        //        //}

        //        //var entry = context.tPLentries
        //        //    .Where(p => p.PLID == idPlaylist && p.SongID == idSong)
        //        //    .FirstOrDefault();

        //        //if (entry == null)
        //        //    throw new ExceptionPlaylistSongNotExist($"Song not exist in the Playlist '{playlist.Name}'");

        //        //context.tPLentries.Remove(entry);
        //        //context.SaveChanges();

        //        return true;
        //    }
        //    catch (ExceptionPlaylistNotExist ex)
        //    {
        //        MessageBox.Show(ex.Message, "ERROR: RemoveSongFromPlaylist");
        //        return false;
        //    }
        //    catch (ExceptionPlaylistSongNotExist ex)
        //    {
        //        MessageBox.Show(ex.Message, "ERROR: RemoveSongFromPlaylist");
        //        return false;
        //    }
        //}
        #endregion

        public static bool DeleteSong(int id)
        {
            try
            {
                //var context = new MyJukeboxEntities();
                //var songs = context.tSongs.First(s => s.ID == id);
                //context.tSongs.Remove(songs);
                //context.SaveChanges();
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

    }
}

