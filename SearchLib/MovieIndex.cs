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

namespace SearchLib
{
    internal class MovieIndex : IDisposable
    {
        private const LuceneVersion MATCH_LUCENE_VERSION = LuceneVersion.LUCENE_48;
        private const int SNIPPET_LENGTH = 100;

        private readonly IndexWriter writer;
        private readonly Analyzer analyzer;
        private readonly QueryParser queryParser;
        private readonly SearcherManager searchManager;
        readonly FSDirectory directory;
        readonly string _indexPath;

        public MovieIndex(string indexPath)
        {
            _indexPath = indexPath;
            analyzer = SetupAnalyzer();
            queryParser = SetupQueryParser(analyzer);
            directory = FSDirectory.Open(indexPath);
            IndexWriterConfig indexWriterConfig = new IndexWriterConfig(MATCH_LUCENE_VERSION, analyzer);


            try
            {
                if (IndexWriter.IsLocked(directory))
                {
                    IndexWriter.Unlock(directory);
                }
                writer = new IndexWriter(directory, indexWriterConfig);
            }

            catch (LockObtainFailedException)
            {
                IndexWriter.Unlock(directory);

                writer = new IndexWriter(directory, indexWriterConfig);
            }
            searchManager = new SearcherManager(writer, true, null);
        }

        public void BuildIndex(IEnumerable<Movie> movies)
        {
            if (movies == null)
            {
                throw new ArgumentNullException();
            }

            for (int i = 0; i < 100; i++)
            {
                if (IndexWriter.IsLocked(directory))
                {
                    IndexWriter.Unlock(directory);
                }
            }


            foreach (var movie in movies)
            {
                Document movieDocument = BuildDocument(movie);
                // writer.DeleteDocuments(new Term("movieid", movie.MovieId.ToString()));
                writer.UpdateDocument(new Term("movieid", movie.MovieId.ToString()), movieDocument);
            }
            writer.Flush(true, true);

            writer.Commit();

            if (IndexWriter.IsLocked(directory))
            {
                IndexWriter.Unlock(directory);
            }

        }
        public void UpdateMovieDocument(Movie movie)
        {
            if (movie == null)
            {
                return;
            }
            Document movieDocument = BuildDocument(movie);
            // writer.DeleteDocuments(new Term("movieid", movie.MovieId.ToString()));
            writer.UpdateDocument(new Term("movieid", movie.MovieId.ToString()), movieDocument);
            writer.Flush(true, true);
            writer.Commit();
        }

        public SearchResults Search(string queryString)
        {
            int resultsPerPage = 10;
            Query query = BuildQuery(queryString);
            searchManager.MaybeRefreshBlocking();
            IndexSearcher searcher = searchManager.Acquire();

            try
            {
                TopDocs topDocs = searcher.Search(query, resultsPerPage);
                return CompileResults(searcher, topDocs);
            }
            finally
            {
                searchManager.Release(searcher);
                searcher = null;
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

        private Query BuildQuery(string queryString)
        {
           // queryString = QueryParserBase.Escape(queryString);
            return queryParser.Parse(queryString);
        }

        private Document BuildDocument(Movie movie)
        {
            Document doc = new Document
            {
                new StoredField("movieid", movie.MovieId),
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

        public void Dispose()
        {
            if (IndexWriter.IsLocked(directory))
            {
                IndexWriter.Unlock(directory);
            }
            
            searchManager?.Dispose();
           
            analyzer?.Dispose();
            writer?.Dispose();
        }
    }
}