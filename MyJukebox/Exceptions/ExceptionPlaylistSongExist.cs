using System;

namespace MyJukeboxWMPDapper.Exceptions
{
    public class ExceptionPlaylistSongExist : Exception
    {
        public ExceptionPlaylistSongExist(string message) : base(message)
        {

        }
    }
}
