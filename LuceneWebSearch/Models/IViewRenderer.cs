namespace LuceneWebSearch.Models
{
    public interface IViewRenderer
    {
        T Render<T>(T descriptionPath, object p);
    }
}