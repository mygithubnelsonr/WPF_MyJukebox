namespace MyJukebox.Common
{
    public class Helpers
    {
        public static string GetQueryString(string queryText)
        {
            return $"select * from vsongs where charindex('{queryText}', lower( concat([pfad],[filename]))) > 0";
        }

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

    }
}
