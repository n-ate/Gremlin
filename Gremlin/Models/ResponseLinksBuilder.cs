using System.Collections.Generic;

namespace n_ate.Gremlin.Models
{
    internal class ResponseLinksBuilder
    {
        internal static readonly ResponseLinksBuilder Empty = new ResponseLinksBuilder();

        internal ResponseLinksBuilder(string requestPath, int currentSkip, int resultsCount, int pageSize)
        {
            RequestPath = requestPath;
            CurrentSkip = currentSkip;
            ResultsCount = resultsCount;
            PageSize = pageSize;
        }

        private ResponseLinksBuilder()
        { }

        internal int CurrentSkip { get; }
        internal int PageSize { get; set; }
        internal string? RequestPath { get; }
        internal int ResultsCount { get; set; }

        internal Dictionary<string, string> ToLinks()
        {
            var results = new Dictionary<string, string>();
            if (this != Empty)
            {
                if (CurrentSkip > 0)
                {
                    var pageStart = CurrentSkip - PageSize;
                    if (pageStart < 0) pageStart = 0;
                    results.Add("previous", $"{RequestPath}?skip={pageStart}&take={PageSize}");
                }
                if (PageSize <= ResultsCount)
                {
                    var pageStart = CurrentSkip + ResultsCount;
                    results.Add("next", $"{RequestPath}?skip={pageStart}&take={ResultsCount}");
                }
            }
            return results;
        }
    }
}