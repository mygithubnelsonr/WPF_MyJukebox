using System.Collections.Generic;

namespace MyJukebox
{
    public class QueryList : List<Query>
    {
        public QueryList()
        {

        }

        public QueryList Load()
        {
            QueryList queries = new QueryList();

            queries.Add(new Query() { ID = 1, Name = "dt-r-01", Row = 4 });
            queries.Add(new Query() { ID = 2, Name = "mavericks", Row = 8 });
            queries.Add(new Query() { ID = 3, Name = "xtra_001", Row = 0 });
            queries.Add(new Query() { ID = 3, Name = "xtra_002", Row = 0 });
            queries.Add(new Query() { ID = 3, Name = "xtra_003", Row = 0 });
            queries.Add(new Query() { ID = 3, Name = "xtra_004", Row = 0 });
            queries.Add(new Query() { ID = 3, Name = "xtra_005", Row = 0 });
            queries.Add(new Query() { ID = 3, Name = "xtra_006", Row = 0 });
            queries.Add(new Query() { ID = 3, Name = "xtra_007", Row = 0 });
            queries.Add(new Query() { ID = 3, Name = "xtra_008", Row = 0 });

            return queries;
        }
    }
}

