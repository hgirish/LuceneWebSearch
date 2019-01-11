using Lucene.Net.Documents;
using System;

namespace LuceneWebSearch.Models
{
    public class SearchResult
    {
        private readonly Document _doc;

        public SearchResult(Document doc)
        {
            _doc = doc;
        }
        public string DescriptionPath { get; set; }

        public string LinkHref { get; set; }

        public string LinkText { get; set; }

        public void Parse(Action<Document> parseAction)
        {
            parseAction(_doc);
        }
    }
}
