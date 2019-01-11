using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;


namespace LuceneWebSearch.Models
{
    public class SearchManager : ISearchManager
    {
        
        public void AddToIndex(params Searchable[] searchables)
        {
            UseWriter(x =>
            {
                foreach (var s in searchables)
                {
                    var doc = new Document();
                    foreach (var field in s.GetFields())
                    {
                        doc.Add(field);
                    }
                    x.AddDocument(doc);
                }
            });
        }

        private void UseWriter(Action<IndexWriter> action)
        {
            using (var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48))
            {
                using (var writer = new IndexWriter(FSDirectory.Open("."),
                    new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)))
                {
                    action(writer);
                    writer.Commit();
                }
            }
        }
    }
}