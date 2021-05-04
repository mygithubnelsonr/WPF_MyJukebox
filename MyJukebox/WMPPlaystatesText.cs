using System.Collections.Generic;

namespace MyJukeboxWMPDapper
{
    public static class WMPStatesText
    {
        private static List<string> _listStates = new List<string>();

        static WMPStatesText()
        {
            _listStates.Add("wmppsUndefined");
            _listStates.Add("wmppsStopped");
            _listStates.Add("wmppsPaused");
            _listStates.Add("wmppsPlaying");
            _listStates.Add("wmppsScanForward");
            _listStates.Add("wmppsBuffering");
            _listStates.Add("wmppsWaiting");
            _listStates.Add("wmppsMediaEnded");
            _listStates.Add("wmppsTransitioning");
            _listStates.Add("wmppsReady");
            _listStates.Add("wmppsReconnecting");
            _listStates.Add("wmppsLast");
        }

        public static string Playstate(int index)
        {
            return _listStates[index];
        }
    }
}
