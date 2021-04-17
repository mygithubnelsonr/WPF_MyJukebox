using System.Windows.Input;

namespace MyJukeboxWMPDapper
{
    public static class MyJukeboxCommands
    {
        private static RoutedUICommand copyDataRow;
        private static RoutedUICommand playbackloop;
        private static RoutedUICommand playbackshuffle;

        static MyJukeboxCommands()
        {
            copyDataRow = new RoutedUICommand("Copy Datarow", "CopyDataRow", typeof(MyJukeboxCommands));
            playbackloop = new RoutedUICommand("Playback Loop", "PlaybackLoop", typeof(MyJukeboxCommands));
            playbackshuffle = new RoutedUICommand("Playback Shuffle", "PlaybackShuffle", typeof(MyJukeboxCommands));
        }

        public static RoutedUICommand PlaybackShuffle
        {
            get { return playbackshuffle; }
        }

        public static RoutedUICommand PlaybackLoop
        {
            get { return playbackloop; }
        }

        public static RoutedUICommand CopyDataRow
        {
            get { return copyDataRow; }
        }
    }
}
