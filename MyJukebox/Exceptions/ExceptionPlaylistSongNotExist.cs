using System;

namespace MyJukeboxWMPDapper.Exceptions
{
    public class ExceptionPlaylistSongNotExist : Exception
    {
        public ExceptionPlaylistSongNotExist(string message) : base(message)
        {

        }
    }
}
