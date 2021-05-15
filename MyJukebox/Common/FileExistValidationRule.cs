using MyJukeboxWMPDapper.DataAccess;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;

namespace MyJukeboxWMPDapper
{
    public class FileExistValidationRule : ValidationRule
    {
        string fullpath = "";

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            try
            {
                var bindingGroup = value as BindingGroup;

                var song = bindingGroup.Items[0] as vSongModel;

                if (song != null)
                    fullpath = Path.Combine(song.Path, song.FileName);

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
