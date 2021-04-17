using System.Collections.Generic;
using System.IO;

namespace MyJukeboxWMPDapper.Common
{
    public class ImageFlipper
    {
        //private static ImageFlipper _imageFlipper = new ImageFlipper();
        //private string _imagePath = "";
        //private string _artist = "";

        #region CTOR
        private ImageFlipper()
        {

        }
        #endregion

        //public static ImageFlipper Instanz()
        //{
        //    return new ImageFlipper();
        //}

        public static List<string> GetImagesFullNames(string fullPath, string artist)
        {
            List<string> artistImageFiles = new List<string>();

            var di = new DirectoryInfo(fullPath);
            if (di.Exists)
            {
                var files = di.GetFiles();
                artistImageFiles.Clear();

                foreach (var file in files)
                {
                    if (true)
                    {
                        if (file.Name.ToLower().IndexOf(artist.ToLower()) > -1)
                            artistImageFiles.Add(file.FullName);
                    }
                }
            }
            return artistImageFiles;
        }

        //public List<BitmapImage> GetImage(string artist)
        //{
        //    List<BitmapImage> images = new List<BitmapImage>();

        //    //if (artistImageFiles.Count == 0)
        //    //{
        //    //    BitmapImage image = new BitmapImage(new Uri("/MyProject;component/Images/down.png", UriKind.Relative));

        //    //    imageArtist.Source = "/Images/ShadowMen.gif";
        //    //    return;
        //    //}

        //    //if (Tag == null || (int)Tag >= artistImageFiles.Count) Tag = 0;

        //    //int counter = (int)Tag;
        //    //imageArtist.ImageLocation = artistImageFiles[counter];
        //    //imageArtist.SizeMode = PictureBoxSizeMode.Zoom;
        //    //Tag = ++counter;

        //    return images;

        //}
    }
}
