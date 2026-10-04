using PagedList;
using System.Collections.Generic;

namespace _25DH110528_MyStore.Models.ViewModel
{
    public class ProductSearchVM
    {
        public IPagedList<Product> Products { get; set; }

        public string SearchTerm { get; set; }

        public decimal? MinPrice { get; set; }  

        public decimal? MaxPrice { get; set; }

        public string SortOrder { get; set; }
    }
}