using System;

namespace MyJukebox.Exceptions
{
    class ExceptionDeleteSong : Exception
    {
        public ExceptionDeleteSong(string message) : base(message)
        {

        }
    }
}
