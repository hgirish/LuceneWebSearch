namespace LuceneWebSearch.Models
{
    public class SearchableArticle : Searchable
    {

        public SearchableArticle(Article article, IViewRenderer viewRenderer)
        {
            var descriptionPath = $"Pages/Articles/Intro{article.Id}";
            DescriptionPath = descriptionPath;
            Description = viewRenderer.Render<string>(descriptionPath, null);
            Href = $"/articles/read/{article.Id}";
            Id = article.Id;
            Title = article.Title;
        }
        public override string Description { get; }
        public override string DescriptionPath { get; }
        public override string Href { get; }
        public override int Id { get; }
        public override string Title { get; }
    }
}