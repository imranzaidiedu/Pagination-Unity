using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GameDevEasy.Pagination.Example
{
    public class PaginationExample : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _displayText;
        private IPaginator<ItemData> _itemsPaginator;
        private IPageProvider<ItemData> _itemsPageProvider;
        
        private void Awake()
        {
            _itemsPageProvider = new ItemsPageProvider(103, 10);
            _itemsPaginator = new ItemsPaginator(_itemsPageProvider);
        }

        private void GetCurrentPage(Action<string> callback)
        {
            _itemsPaginator.GetCurrentPage(page =>
            {
                if(page != null)
                {
                    callback?.Invoke($"Current Page: {page.ToString()}");
                }
                else
                {
                    callback?.Invoke("No current page found.");
                }
            });
        }
        
        private void GetNextPage(Action<string> callback)
        {
            _itemsPaginator.GetNextPage(page =>
            {
                if(page != null)
                {
                    callback?.Invoke($"Next Page: {page.ToString()}");
                }
                else
                {
                    callback?.Invoke("No next page found.");
                }
            });
        }
        
        private void GetPreviousPage(Action<string> callback)
        {
            _itemsPaginator.GetPreviousPage(page =>
            {
                if(page != null)
                {
                    callback?.Invoke($"Previous Page: {page.ToString()}");
                }
                else
                {
                    callback?.Invoke("No previous page found.");
                }
            });
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                GetCurrentPage(DisplayText);
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                GetNextPage(DisplayText);
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                GetPreviousPage(DisplayText);
            }
        }

        private void DisplayText(string text)
        {
            if (_displayText)
            {
                _displayText.SetText(text);
            }
        }
    }
    
    public class ItemData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        public ItemData(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return $"Id: {Id}, Name: {Name}";
        }
    }
    public class ItemsPage : IPage<ItemData>
    {
        public int PageNumber { get; private set; }
        public int PageSize { get; private set; }
        public ItemData[] Items { get; private set; }

        public ItemsPage(int pageNumber, int pageSize, ItemData[] items)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            Items = new ItemData[items.Length];
            Array.Copy(items, Items, items.Length);
        }

        public override string ToString()
        {
            string str = string.Join(",", Array.ConvertAll(Items, item => item.ToString()));
            return $"PageNumber: {PageNumber}, PageSize: {PageSize}, Items: {str}";
        }
    }
    public class ItemsPageProvider : IPageProvider<ItemData>
    {
        private List<IPage<ItemData>> _allPages;
        private readonly int _totalItems;
        private readonly int _itemsPerPage;

        public ItemsPageProvider(int totalItems, int itemsPerPage)
        {
            _totalItems = totalItems;
            _itemsPerPage = itemsPerPage;
            
            List<ItemData> allItems = new List<ItemData>();
            _allPages = new List<IPage<ItemData>>();
            for (int i = 0; i < _totalItems; i++)
            {
                if(i % _itemsPerPage == 0)
                {
                    if(i != 0)
                    {
                        _allPages.Add(new ItemsPage(i / _itemsPerPage -1 , _itemsPerPage, allItems.ToArray()));
                        allItems.Clear();
                    }
                }
                allItems.Add(new ItemData(i, "Item " + (i + 1)));
            }
            
            if(allItems.Count > 0)
            {
                _allPages.Add(new ItemsPage(Mathf.FloorToInt((float)totalItems / _itemsPerPage), _itemsPerPage, allItems.ToArray()));
            }
        }

        public void FetchPage(int pageNumber, Action<IPage<ItemData>> onSuccess, Action<Exception> onError)
        {
            if (pageNumber < _allPages.Count)
            {
                onSuccess?.Invoke(_allPages[pageNumber]);
                return;
            }
            
            onSuccess?.Invoke(null);
        }

        public bool CanFetchPage(int pageNumber)
        {
            return (pageNumber - 1) * _itemsPerPage < _totalItems;
        }
    }
    public class ItemsPaginator : Paginator<ItemData>
    {
        public ItemsPaginator(IPageProvider<ItemData> pageProvider, int preloadPages = 0) : base(pageProvider, preloadPages)
        {
        }
    }
}