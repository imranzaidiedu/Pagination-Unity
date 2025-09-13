using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDevEasy.PaginationUnity
{
	public class Paginator<T> : IPaginator<T>
	{
		protected virtual int PageNumberStartsFrom { get; set; } = 0;
		private readonly Dictionary<int, IPage<T>> _pagesByNumber;
		private readonly int _preloadPageCount;
		private readonly int _maxPageCacheSize;
		private int _currentPageNumber;
		private readonly IPageProvider<T> _pageProvider;
		private LinkedList<int> _pageCache;
		private bool _initialized;
		
		private bool IsCaching => _maxPageCacheSize > 1;

		protected Paginator(IPageProvider<T> pageProvider, int preloadPageCount = 0, int maxPageCacheSize = 0)
		{
			if (IsCaching && preloadPageCount > maxPageCacheSize)
			{
				throw new ArgumentException($"Preload page count ({preloadPageCount}) cannot be greater than max page cache count ({maxPageCacheSize}). " +
					$"Please set preloadPageCount to a value less than or equal to maxPageCacheCount.");
			}
			
			_pagesByNumber = new  Dictionary<int, IPage<T>>();
			_pageProvider = pageProvider;
			_maxPageCacheSize = maxPageCacheSize;
			_preloadPageCount = preloadPageCount;
			_currentPageNumber = PageNumberStartsFrom;
			if (IsCaching){ _pageCache = new LinkedList<int>(); }

			Initialize();
		}
		
		private void Initialize()
		{
			_initialized = false;
			
			_pageProvider.FetchPage(_currentPageNumber, page =>
			{
				_initialized = true;
				if (page == null)
				{
					Debug.Log($"No data available at all.");
					return;
				}
				
				if (_pagesByNumber.TryAdd(_currentPageNumber, page))
				{
					//First page is already cached, start with the 2nd page.
					PreloadPages(_currentPageNumber + 1);
				}
				else
				{
					Debug.LogError("Error occured while adding the first page to dictionary");
				}
			}, exception =>
			{
				LogExceptionCaughtWhileFetchingPageNumber(_currentPageNumber,  exception);
			});
		}
		
		private void PreloadPages(int preloadPageNumber)
		{
			if (preloadPageNumber > _preloadPageCount)
			{
				Debug.Log($"Preloaded the pages");
				return;
			}

			if (!TryGetPage(preloadPageNumber, out IPage<T> page))
			{
				if (!_pageProvider.CanFetchPage(preloadPageNumber))
				{
					Debug.Log($"Preloaded the pages");
					return;
				}

				_pageProvider.FetchPage(preloadPageNumber, fetchedPage =>
				{
					if (fetchedPage == null)
					{
						Debug.Log($"No data available for {preloadPageNumber}th page");
						return;
					}

					if (!_pagesByNumber.TryAdd(preloadPageNumber, fetchedPage))
					{
						Debug.LogError("Error occured while adding page to dictionary");
						return;
					}

					PreloadNextPage(preloadPageNumber);
				}, exception =>
				{
					PreloadNextPage(preloadPageNumber);
					LogExceptionCaughtWhileFetchingPageNumber(preloadPageNumber, exception);
				});
			}
			else
			{
				PreloadNextPage(preloadPageNumber);
			}
		}

		private void PreloadNextPage(int pageNumber)
		{
			PreloadPages(pageNumber + 1);
		}

		public void GetCurrentPage(Action<IPage<T>> callback)
		{
			GetAdjacentPage(GetCurrentPageNumber(), GetCurrentPageNumber(), callback);
		}
		
		public void GetNextPage(Action<IPage<T>> callback)
		{
			GetAdjacentPage(GetCurrentPageNumber(), GetCurrentPageNumber() + 1, callback);
		}
		
		public void GetPreviousPage(Action<IPage<T>> callback)
		{
			GetAdjacentPage(GetCurrentPageNumber(), GetCurrentPageNumber() - 1, callback);
		}
		
		private void GetAdjacentPage(int currentPageNumber, int nextPageNumber, Action<IPage<T>> callback)
		{
			if (!_initialized) { return; }

			if (nextPageNumber < PageNumberStartsFrom)
			{
				Debug.Log("Pagination: No previous page when you are at first page.");
				callback?.Invoke(null);
				return;
			}

			if (!_pageProvider.CanFetchPage(nextPageNumber))
			{
				Debug.Log("No page available for page number: " + nextPageNumber);
				callback?.Invoke(null);
				return;
			}

			if (!TryGetPage(currentPageNumber, out IPage<T> currentPage))
			{
				Debug.LogError("Pagination: Current page doesn't exist.");
				callback?.Invoke(null);
				return;
			}

			if (TryGetPage(nextPageNumber, out IPage<T> page))
			{
				SavePageAtNumber(nextPageNumber, page);
				callback?.Invoke(page);
				return;
			}

			FetchAndSavePage(nextPageNumber, callback);
		}
		
		private void FetchAndSavePage(int pageNumber, Action<IPage<T>> callback)
		{
			if (_pageProvider.CanFetchPage(pageNumber))
			{
				_pageProvider.FetchPage(pageNumber, page =>
				{
					if (page == null)
					{
						Debug.Log($"No data available for {pageNumber}th page.");
						callback?.Invoke(null);
						return;
					}
					
					SavePageAtNumber(pageNumber, page);
					callback?.Invoke(page);
				}, exception =>
				{
					callback?.Invoke(null);
					LogExceptionCaughtWhileFetchingPageNumber(pageNumber, exception);
				});
			}
		}
		
		private void SavePageAtNumber(int pageNumber, IPage<T> page)
		{
			SetCurrentPageNumber(pageNumber);
			if (!IsCaching)
			{
				_pagesByNumber[pageNumber] = page;
				return;
			}

			// If already in cache, update value and move to most recent
			if (_pagesByNumber.ContainsKey(pageNumber))
			{
				_pagesByNumber[pageNumber] = page;
				_pageCache.Remove(pageNumber);
				_pageCache.AddLast(pageNumber);
				return;
			}
			
			// If cache is full, evict least recently used
			if (_pagesByNumber.Count >= _maxPageCacheSize)
			{
				int oldest = _pageCache.First.Value;
				_pageCache.RemoveFirst();
				_pagesByNumber.Remove(oldest);
			}
			_pagesByNumber[pageNumber] = page;
			_pageCache.AddLast(pageNumber);
		}
		
		private bool TryGetPage(int pageNumber, out IPage<T> page)
		{
			if (!IsCaching)
			{
				return _pagesByNumber.TryGetValue(pageNumber, out page);
			}
			
			if (_pagesByNumber.TryGetValue(pageNumber, out page))
			{
				// Move to most recent
				_pageCache.Remove(pageNumber);
				_pageCache.AddLast(pageNumber);
				return true;
			}
			return false;
		}
		
		private void SetCurrentPageNumber(int pageNumber)
		{
			_currentPageNumber = pageNumber;
		}
		
		private int GetCurrentPageNumber()
		{
			return _currentPageNumber;
		}

		private void LogExceptionCaughtWhileFetchingPageNumber(int pageNumber, Exception exception)
		{
			Debug.LogError($"Caught an exception while fetching the {pageNumber}th page. Exception: {exception}. Stack Trace: {exception.StackTrace}");
		}
	}
}