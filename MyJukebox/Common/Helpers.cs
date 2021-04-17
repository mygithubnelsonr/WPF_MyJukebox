using MyJukeboxWMPDapper.DataAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MyJukeboxWMPDapper.Common
{
    public class Helpers
    {
        public static string GetContainer(string path)
        {
            string container = "";

            // check if network share or drive letter
            if (path.StartsWith(@"\\"))
            {
                string b = path.Remove(0, 2);
                int start = b.IndexOf("\\") + 1;
                int len = b.Length - start;
                container = b.Substring(start, len);
            }
            else
            {
                string b = path.Remove(0, 3);
                container = b;
            }

            return container;
        }

        public static string GetShortPath(string fullpath)
        {
            string shortpath = "";

            var ar = fullpath.Split('\\');
            if (ar[0] == "" && ar[1] == "")    // shared folder
                shortpath = String.Join("\\", ar[0], ar[1], ar[2], ar[3]);
            else
                shortpath = String.Join("\\", ar[0], ar[1], ar[2], ar[3]);

            return shortpath;
        }

        public static string GetImagePath()
        {
            string rootPath = GetSetData.GetSetting("RootImagePath");
            string imagePath = GetSetData.GetSetting("ImagePath");
            string fullpath = Path.Combine(
                rootPath,
                AudioStates.Genre,
                imagePath);
            return fullpath;
        }

        public static void CheckSongPathExist()
        {
            List<string> notValidPath = new List<string>();
            Views.MyMessageBox messageBox = new Views.MyMessageBox();

            var pathlist = GetSetData.GetSongPathList();

            bool found = true;

            if (pathlist != null)
            {
                foreach (var p in pathlist)
                {
                    if (!Directory.Exists(p))
                    {
                        notValidPath.Add(p);
                        found = false;
                    }
                }
            }
            else
            {
                found = false;
                messageBox.MMessage = $"Pathlist is empty!";
            }

            if (found == false)
            {
                messageBox.MMessage = $"This path was not found!\n{String.Join(Environment.NewLine, notValidPath)}";
            }
            else
            {
                messageBox.MMessage = $"All path are valid.";
            }

            messageBox.MTitle = "Check Song Path";
            messageBox.ShowDialog();

        }

        public static bool CheckSongPathExistAsync()
        {
            List<string> notValidPath = new List<string>();

            var pathlist = GetSetData.GetSongPathList();

            bool found = true;

            if (pathlist != null)
            {
                foreach (var p in pathlist)
                {
                    Debug.Print($"path={p}");
                    if (!Directory.Exists(p))
                    {
                        Debug.Print(p);
                        found = false;
                        break;
                    }
                }
            }
            else
                found = false;

            if (found == false)
            {
                Views.MyMessageBox messageBox = new Views.MyMessageBox();
                messageBox.MTitle = "Check Song Path";
                messageBox.MMessage = "No song path found!\n\nExit Application.";
                var result = messageBox.ShowDialog();
            }

            return found;
        }

    }
}
