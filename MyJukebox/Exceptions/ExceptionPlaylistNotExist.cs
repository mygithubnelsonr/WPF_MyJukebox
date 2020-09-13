using System;

namespace MyJukebox.Exceptions
{
    public class ExceptionPlaylistNotExist : Exception
    {
        public ExceptionPlaylistNotExist(string message) : base(message)
        {

        }
    }
}

