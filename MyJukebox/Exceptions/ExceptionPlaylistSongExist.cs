using System;

namespace MyJukebox.Exceptions
{
    public class ExceptionPlaylistSongExist : Exception
    {
        public ExceptionPlaylistSongExist(string message) : base(message)
        {

        }
    }
}
