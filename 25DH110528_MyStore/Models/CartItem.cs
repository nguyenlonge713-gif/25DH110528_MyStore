using System;

namespace _25DH110528_MyStore.Models
{
    public class CartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        // Tính thành tiền cho từng sản phẩm
        public decimal TotalPrice
        {
            get { return Quantity * UnitPrice; }
        }
    }
}