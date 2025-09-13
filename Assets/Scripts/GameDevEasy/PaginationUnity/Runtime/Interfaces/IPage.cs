namespace GameDevEasy.PaginationUnity
{
    public interface IPage<T>
    {
        int PageNumber { get; }
        int PageSize { get; }
        T[] Items { get; }
    }
}