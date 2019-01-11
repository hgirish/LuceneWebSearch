namespace SearchLib
{
    public interface ISearchEngine
    {
        void BuildIndex();
        SearchResults Search(string query);
        void UpdateMovie(Movie movie);
        bool ClearLuceneIndex();
        void ClearLuceneIndexRecord(int movieId);
    }
}
