namespace LuceneWebSearch.Models
{
    public interface ISearchManager
    {
        void AddToIndex(params Searchable[] searchables);
    }
}