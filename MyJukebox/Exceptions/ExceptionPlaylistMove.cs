using System;

namespace MyJukebox.Exceptions
{
    class ExceptionPlaylistMove : Exception
    {
        public ExceptionPlaylistMove(string message) : base(message)
        {

        }
    }
}
