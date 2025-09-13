namespace GameDevEasy.Pagination
{
    public interface IPage<T>
    {
        int PageNumber { get; }
        int PageSize { get; }
        T[] Items { get; }
    }
}