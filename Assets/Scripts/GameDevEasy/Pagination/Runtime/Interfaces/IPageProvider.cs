using System;

namespace GameDevEasy.Pagination
{
    public interface IPageProvider<T>
    {
        void FetchPage(int pageNumber, Action<IPage<T>> onSuccess, Action<Exception> onError);
        bool CanFetchPage(int pageNumber);
    }
}