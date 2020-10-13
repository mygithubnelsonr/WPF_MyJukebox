using MyJukebox.BLL;
using MyJukebox.DAL;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;

namespace MyJukebox
{
    public class FileExistValidationRule : ValidationRule
    {
        string fullpath = "";

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            try
            {
                var bindingGroup = value as BindingGroup;
                if (DataGetSet.Datasource == DataGetSet.DataSourceEnum.Songs ||
                    DataGetSet.Datasource == DataGetSet.DataSourceEnum.Query
                    )
                {
                    var song = bindingGroup.Items[0] as vSong;
                    fullpath = Path.Combine(song.Pfad, song.FileName);
                }
                else
                {
                    var song = bindingGroup.Items[0] as vPlaylistSong;
                    fullpath = Path.Combine(song.Pfad, song.FileName);
                }

                if (!File.Exists(fullpath))
                    return new ValidationResult(false, "File not found!");

                return ValidationResult.ValidResult;

            }
            catch
            {
                return null;
            }
        }
    }
}
