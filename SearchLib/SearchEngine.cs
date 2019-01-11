using Lucene.Net.Index;
using Lucene.Net.Store;
using System.Text.RegularExpressions;

namespace SearchLib
{
    public class SearchEngine : ISearchEngine
    {
        private readonly MovieIndex index;
        private readonly string _indexLocation;

        public SearchEngine(string indexLocation)
        {
            index = new MovieIndex(indexLocation);
            _indexLocation = indexLocation;
        }

        public void ReleaseWriterLock(string indexLocation)
        {
            var directory = FSDirectory.Open(_indexLocation);
            if (IndexWriter.IsLocked(directory))
            {
                  IndexWriter.Unlock(directory);
               
               
            }

        }
        public void UpdateMovie(Movie movie)
        {
             index.UpdateMovieDocument(movie);
        }
        public void BuildIndex() => index.BuildIndex(Repository.GetMoviesFromFile());

        public SearchResults Search(string query)
        {
            query = Regex.Replace(query, "[^0-9a-zA-Z -]+", "");
            var results = index.Search(query);

            if (results.Hits.Count == 0 && !query.EndsWith('~'))
            {
                results = index.Search($"{ query}~");
            }
            return results;
        }

    }
}
