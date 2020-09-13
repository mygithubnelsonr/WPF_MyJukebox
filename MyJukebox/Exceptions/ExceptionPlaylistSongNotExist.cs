using System;

namespace MyJukebox.Exceptions
{
    public class ExceptionPlaylistSongNotExist : Exception
    {
        public ExceptionPlaylistSongNotExist(string message) : base(message)
        {

        }
    }
}
