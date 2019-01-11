using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SearchLib
{
    internal class MovieIndex : IDisposable
    {
        private const LuceneVersion MATCH_LUCENE_VERSION = LuceneVersion.LUCENE_48;
        private const int SNIPPET_LENGTH = 100;

        private  IndexWriter _writer;
        IndexWriter writer
        {
            get
            {
                if (_writer == null)
                {
                    IndexWriterConfig indexWriterConfig =
                                new IndexWriterConfig(MATCH_LUCENE_VERSION, SetupAnalyzer());
                    _writer = new IndexWriter(_directory, indexWriterConfig);
                }
                return _writer;
            }
        }
     //   private readonly Analyzer analyzer;
       // private readonly QueryParser queryParser;
     //   private readonly SearcherManager searchManager;
        private FSDirectory _tempDirectory;

         FSDirectory _directory
        {
            get
            {
                if (_tempDirectory == null)
                {
                    _tempDirectory = FSDirectory.Open(_indexPath);                 
                }
                if (IndexWriter.IsLocked(_tempDirectory))
                {
                    IndexWriter.Unlock(_tempDirectory);
                }
                var lockFilePath = System.IO.Path.Combine(_indexPath, "write.lock");
                if (System.IO.File.Exists(lockFilePath))
                {
                    System.IO.File.Delete(lockFilePath);
                }
                return _tempDirectory;
            }
        }
        readonly string _indexPath;

        public MovieIndex(string indexPath)
        {
            _indexPath = indexPath;
           // analyzer = SetupAnalyzer();
         //   queryParser = SetupQueryParser(analyzer);

          //  searchManager = new SearcherManager(writer, true, null);

         

        }



        public void BuildIndex(IEnumerable<Movie> movies)
        {
            if (movies == null)
            {
                throw new ArgumentNullException();
            }

            Analyzer analyzer = SetupAnalyzer();

            using (var writer = new IndexWriter(_directory,
                new IndexWriterConfig(MATCH_LUCENE_VERSION, analyzer)))
            {
                foreach (var movie in movies)
                {
                    AddToIndex(writer, movie);
                }

                writer.Flush(true, true);
                writer.Commit();
                analyzer.Dispose();
                writer.Dispose();
            }



            ReleaseWriteLock();

        }

        private void AddToIndex(IndexWriter writer, Movie movie)
        {
            var searchQuery = new TermQuery(new Term("movieid", movie.MovieId.ToString()));
            writer.DeleteDocuments(searchQuery);

            Document movieDocument = BuildDocument(movie);
            writer.UpdateDocument(new Term("movieid",
                movie.MovieId.ToString()), movieDocument);
        }

        public void UpdateMovieDocument(Movie movie)
        {
            if (movie == null)
            {
                return;
            }
            Document movieDocument = BuildDocument(movie);
            writer.UpdateDocument(new Term("movieid", movie.MovieId.ToString()), movieDocument);
            writer.Flush(true, true);
            writer.Commit();
        }

        public SearchResults Search(string queryString)
        {
            int resultsPerPage = 10;
            var analyzer = SetupAnalyzer();
            var queryParser = SetupQueryParser(analyzer);
            Query query = BuildQuery(queryString,queryParser);
           

            using (var writer = new IndexWriter(_directory,
                new IndexWriterConfig(MATCH_LUCENE_VERSION, analyzer)))
            {
                var searchManager = new SearcherManager(writer, true, null);
                searchManager.MaybeRefreshBlocking();
                IndexSearcher searcher = searchManager.Acquire();

                try
                {
                    TopDocs topDocs = searcher.Search(query, resultsPerPage);
                    return CompileResults(searcher, topDocs);
                }
                finally
                {
                    searchManager?.Release(searcher);
                    searchManager?.Dispose();
                    searcher = null;
                    analyzer?.Dispose();
                    ReleaseWriteLock();
                }
            }
   
        }

        private SearchResults CompileResults(IndexSearcher searcher, TopDocs topDocs)
        {
            SearchResults results = new SearchResults
            {
                TotalHits = topDocs.TotalHits
            };
            foreach (var s in topDocs.ScoreDocs)
            {
                Document document = searcher.Doc(s.Doc);
                Hit searchResult = new Hit
                {
                    Rating = document.GetField("rating")?.GetStringValue(),
                    MovieId = document.GetField("movieid")?.GetStringValue(),
                    Score = s.Score,
                    Title = document.GetField("title")?.GetStringValue(),
                    Snippet = document.GetField("snippet")?.GetStringValue(),

                };

                results.Hits.Add(searchResult);
            }
            return results;
        }

        private Query BuildQuery(string queryString, QueryParser parser)
        {
            Query  query ;
            if (string.IsNullOrEmpty(queryString))
            {
                return null;
            }
            queryString = queryString.Trim();
            try
            {
                query = parser.Parse(queryString);
            }
            catch (ParseException ex)
            {
                Console.WriteLine(ex.Message);
                query = parser.Parse(QueryParser.Escape(queryString));
            }
            return query;
          
        }

        private Document BuildDocument(Movie movie)
        {
            Document doc = new Document
            {
                // OBSOLETE
                // new Field("movieid",movie.MovieId.ToString(),store:Field.Store.YES, index:Field.Index.NOT_ANALYZED),
                // DOES NOT STORE- SO CANNOT DELETE
                // new StoredField("movieid", movie.MovieId),
               new StringField("movieid", movie.MovieId.ToString(),Field.Store.YES),
                new TextField("title", movie.Title, Field.Store.YES),
                new TextField("description", movie.Description, Field.Store.NO),
                new StoredField("snippet", MakeSnippet(movie.Description)),
                new StringField("rating", movie.Rating, Field.Store.YES)
            };
            return doc;
        }

        private string MakeSnippet(string description)
        {
            return (string.IsNullOrEmpty(description) || description.Length <= SNIPPET_LENGTH)
                ? description
                : $"{description.Substring(0, SNIPPET_LENGTH)}...";
        }

        private Analyzer SetupAnalyzer()
        {
            return new StandardAnalyzer(MATCH_LUCENE_VERSION, StandardAnalyzer.STOP_WORDS_SET);
        }
        private QueryParser SetupQueryParser(Analyzer analyzer)
        {
            return new MultiFieldQueryParser(MATCH_LUCENE_VERSION,
                new[] { "title", "description" },
                analyzer);
        }


        public  void ClearLuceneIndexRecord(int record_id)
        {
            // init lucene
            var analyzer = SetupAnalyzer();
            using (var writer = new IndexWriter(_directory,
            new IndexWriterConfig(MATCH_LUCENE_VERSION, analyzer)))
            {
                // remove older index entry
                var searchQuery = new TermQuery(new Term("movieid", record_id.ToString()));
                writer.DeleteDocuments(searchQuery);

                // close handles
                writer.Flush(true, true);
                writer.Commit();
                analyzer.Dispose();

                writer.Dispose();
                
            }
            ReleaseWriteLock();
        }
        private void ReleaseWriteLock()
        {
            if (IndexWriter.IsLocked(_directory))
            {
                IndexWriter.Unlock(_directory);
            }
        }
        public  bool ClearLuceneIndex()
        {
            try
            {
                var analyzer = SetupAnalyzer();
                using (var writer = new IndexWriter(_directory,
                new IndexWriterConfig(MATCH_LUCENE_VERSION, analyzer)))
                {
                    // remove older index entries
                    writer.DeleteAll();

                    // close handles
                    writer.Flush(true, true);
                    writer.Commit();
                    analyzer.Dispose();
                    writer.Dispose();
                }
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                ReleaseWriteLock();
            }
            return true;
        }
        public void Dispose()
        {
            ReleaseWriteLock();
            
           // searchManager?.Dispose();
           
          //  analyzer?.Dispose();
            writer?.Dispose();
        }
    }
}
