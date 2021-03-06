using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
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
                                new IndexWriterConfig(MATCH_LUCENE_VERSION, SetupAnalyzer()) { OpenMode = OpenMode.CREATE_OR_APPEND};
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
            IEnumerable<FieldDefinition> fields = new List<FieldDefinition> {
                new FieldDefinition{Name="title", isDefault=true},
                new FieldDefinition{Name="description", isDefault=false }
            };
            // Query query = BuildQuery(queryString,queryParser); // BuildQuery(queryString, fields); // 
            Query query;
            if (queryString.EndsWith('~'))
            {
                query = BuildQuery(queryString, queryParser);
            }
           else
            {
                query = BuildQuery(queryString, fields);
            }

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

        private IList<string> Tokenize(string userInput, string field)
        {
            List<string> tokens = new List<string>();
            var analyzer = SetupAnalyzer();
            using (var reader = new StringReader(userInput))
            {
                using (TokenStream stream = analyzer.GetTokenStream(field, reader))
                {
                    stream.Reset();
                    while (stream.IncrementToken())
                    {
                        tokens.Add(stream.GetAttribute<ICharTermAttribute>().ToString());
                    }
                }
            }
            return tokens;
        }
        
        private Analyzer SetupAnalyzer()
        {
            return new EnhancedEnglishAnalyzer(MATCH_LUCENE_VERSION, StandardAnalyzer.STOP_WORDS_SET);
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

        class FieldDefinition
        {
            public string Name { get; set; }
            public bool isDefault { get; set; } = false;
        }
        private Query BuildQuery(string userInput, IEnumerable<FieldDefinition> fields)
        {
            BooleanQuery query = new BooleanQuery();
            
            var tokens = Tokenize(userInput, "title");

            if (tokens.Count > 1)
            {
                FieldDefinition defaultField = fields.FirstOrDefault(f => f.isDefault == true);
                query.Add(BuildExactPhaseQuery(tokens, defaultField), Occur.SHOULD);

                foreach (var q in GetIncrementalMatchQuery(tokens, defaultField))
                {
                    query.Add(q, Occur.SHOULD);
                }
            }

            //create a term query per field - non boosted
            foreach (var token in tokens)
            {
                foreach (var field in fields)
                {
                    query.Add(new TermQuery(new Term(field.Name, token)), Occur.SHOULD);
                }
            }

            return query;

        }

        private IEnumerable<Query> GetIncrementalMatchQuery(IList<string> tokens, FieldDefinition field)
        {
            BooleanQuery bq = new BooleanQuery();
            foreach (var token in tokens)
                bq.Add(new TermQuery(new Term(field.Name, token)), Occur.SHOULD);

            //5 comes from config - code omitted
            int upperLimit = Math.Min(tokens.Count, 5);
            for (int match = 2; match <= upperLimit; match++)
            {
                BooleanQuery q = bq.Clone() as BooleanQuery;
                q.Boost = match * 3;
                q.MinimumNumberShouldMatch = match;
                yield return q;
            }
        }

        private Query BuildExactPhaseQuery(IList<string> tokens, FieldDefinition field)
        {
            //boost factor (6) and slop (2) come from configuration - code omitted for simplicity
            PhraseQuery pq = new PhraseQuery() { Boost = tokens.Count * 6, Slop = 2 };
            foreach (var token in tokens)
                pq.Add(new Term(field.Name, token));

            return pq;
        }
    }
}
