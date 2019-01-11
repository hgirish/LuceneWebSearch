using Lucene.Net.Index;
using Lucene.Net.Store;
using System.Linq;
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
            var files = System.IO.Directory.GetFiles(indexLocation);
            var indexFiles = files.Where(x => !x.EndsWith("write.lock"));
            var fileCount = indexFiles.Count();

            var filesExist = fileCount > 0; // !System.IO.Directory.EnumerateFiles(indexLocation).Any(x=> !x.EndsWith("write.lock"));
            if (!filesExist)
            {
                BuildIndex();
            }
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

        public bool ClearLuceneIndex()
        {
           return  index.ClearLuceneIndex();
        }

        public void ClearLuceneIndexRecord(int movieId)
        {
            index.ClearLuceneIndexRecord(movieId);
        }
    }
}
