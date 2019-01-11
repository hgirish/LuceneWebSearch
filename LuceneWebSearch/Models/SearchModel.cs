namespace LuceneWebSearch.Models
{
    public class SearchModel
    {
        public SearchResultCollection Results { get; set; }
        public string Search { get; set; }
        public SearchModel()
        {
            Results = new SearchResultCollection();
        }
    }
}
