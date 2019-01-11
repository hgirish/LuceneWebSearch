using Lucene.Net.Documents;
using Lucene.Net.Index;
using System.Collections.Generic;

namespace LuceneWebSearch.Models
{
    public abstract class Searchable
    {
        public static readonly Dictionary<Fields, string> FieldStrings = new Dictionary<Fields, string>
        {
             {Fields.Description, "Description"},
            {Fields.DescriptionPath, "DescriptionPath"},
            {Fields.Href, "Href"},
            {Fields.Id, "Id"},
            {Fields.Title, "Title"}
        };

        public static readonly Dictionary<Fields, string> AnalyzedFields = new Dictionary<Fields, string>
        {
            {Fields.Description, FieldStrings[Fields.Description] },
            {Fields.Title, FieldStrings[Fields.Title] }
        };

        public abstract string Description { get; }
        public abstract string DescriptionPath { get; }
        public abstract string Href { get; }
        public abstract int Id { get; }
        public abstract string Title { get; }

        public enum Fields
        {
            Description,
            DescriptionPath,
            Href,
            Id,
            Title
        }

        public IEnumerable<IIndexableField> GetFields()
        {
            return new Field[]
            {
                new TextField(AnalyzedFields[Fields.Description],
                Description,
                Field.Store.NO),
                 new TextField(AnalyzedFields[Fields.Title],
                 Title,
                 Field.Store.YES){ Boost = 4.0f },
                new StringField(FieldStrings[Fields.Id],
                Id.ToString(),
                Field.Store.YES),
                new StringField(FieldStrings[Fields.DescriptionPath],
                DescriptionPath,
                Field.Store.YES),
                new StringField(FieldStrings[Fields.Href],
                Href,
                Field.Store.YES)

            };

        }
    }
}
