using System;

namespace GameDevEasy.Pagination
{
    public interface IPaginator<T>
    {
        public void GetCurrentPage(Action<IPage<T>> callback);
		public void GetNextPage(Action<IPage<T>> callback);
		public void GetPreviousPage(Action<IPage<T>> callback);
    }
}