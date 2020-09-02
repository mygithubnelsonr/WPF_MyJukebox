namespace MyJukebox.Common
{
    public class Helpers
    {
        public static string GetQueryString(string queryText)
        {
            return $"select * from vsongs where charindex('{queryText}', lower( concat([pfad],[filename]))) > 0";
        }
    }
}
