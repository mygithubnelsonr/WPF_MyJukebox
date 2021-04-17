using System;

namespace MyJukeboxWMPDapper.Exceptions
{
    public class ExceptionPlaylistNotExist : Exception
    {
        public ExceptionPlaylistNotExist(string message) : base(message)
        {

        }
    }
}

