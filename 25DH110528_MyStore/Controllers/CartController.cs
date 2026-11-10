
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using _25DH110528_MyStore.Models;

namespace _25DH110528_MyStore.Controllers
{
    public class CartController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // Bộ nhớ lưu trữ giỏ hàng tạm theo Username
        private static Dictionary<string, List<CartItem>> UserCarts =
            new Dictionary<string, List<CartItem>>();

        // Đồng bộ giỏ hàng theo Username
        public static void SyncUserCart(string username, List<CartItem> cart)
        {
            if (!string.IsNullOrEmpty(username))
            {
                UserCarts[username] = cart;
            }
        }

        // Lấy giỏ hàng đã lưu của User
        public static List<CartItem> GetUserCart(string username)
        {
            if (!string.IsNullOrEmpty(username) &&
                UserCarts.ContainsKey(username))
            {
                return UserCarts[username];
            }

            return null;
        }

        // Lấy giỏ hàng từ Session hoặc bộ nhớ đệm
        private List<CartItem> GetCart()
        {
            List<CartItem> cart = Session["Cart"] as List<CartItem>;
            string username = Session["Username"] as string;

            if (cart == null)
            {
                if (!string.IsNullOrEmpty(username) &&
                    UserCarts.ContainsKey(username))
                {
                    cart = UserCarts[username];
                }
                else
                {
                    cart = new List<CartItem>();
                }

                Session["Cart"] = cart;
            }

            if (!string.IsNullOrEmpty(username))
            {
                UserCarts[username] = cart;
            }

            Session["CartCount"] = cart.Sum(item => item.Quantity);

            return cart;
        }

        // Lưu và đồng bộ giỏ hàng
        private void SaveCart(List<CartItem> cart)
        {
            Session["Cart"] = cart;
            Session["CartCount"] = cart.Sum(item => item.Quantity);

            string username = Session["Username"] as string;

            if (!string.IsNullOrEmpty(username))
            {
                UserCarts[username] = cart;
            }
        }

        // 1. TRANG XEM GIỎ HÀNG
        public ActionResult Index()
        {
            List<CartItem> cart = GetCart();

            ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);

            return View(cart);
        }

        // 2. THÊM SẢN PHẨM VÀO GIỎ HÀNG BẰNG AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int id, int quantity = 1)
        {
            var product = db.Products.Find(id);

            if (product == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy sản phẩm."
                });
            }

            if (quantity < 1)
            {
                quantity = 1;
            }

            List<CartItem> cart = GetCart();

            CartItem item = cart.FirstOrDefault(p => p.ProductID == id);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    ProductImage = product.ProductImage,
                    UnitPrice = Convert.ToDecimal(product.ProductPrice),
                    Quantity = quantity
                });
            }
            else
            {
                item.Quantity += quantity;
            }

            SaveCart(cart);

            return Json(new
            {
                success = true,
                message = "Đã thêm sản phẩm vào giỏ hàng thành công!",
                cartCount = cart.Sum(p => p.Quantity),
                productCount = cart.Count
            });
        }

        // 3. CẬP NHẬT SỐ LƯỢNG SẢN PHẨM
        [HttpPost]
        public ActionResult UpdateCart(int id, int quantity)
        {
            List<CartItem> cart = GetCart();

            CartItem item = cart.FirstOrDefault(p => p.ProductID == id);

            if (item != null)
            {
                if (quantity > 0)
                {
                    item.Quantity = quantity;
                }
                else
                {
                    cart.Remove(item);
                }
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        // 4. XÓA SẢN PHẨM KHỎI GIỎ
        public ActionResult RemoveFromCart(int id)
        {
            List<CartItem> cart = GetCart();

            CartItem item = cart.FirstOrDefault(p => p.ProductID == id);

            if (item != null)
            {
                cart.Remove(item);
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        // 5. TRANG THANH TOÁN
        [HttpGet]
        public ActionResult Checkout()
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            List<CartItem> cart = GetCart();

            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);

            return View(cart);
        }

        // 6. XỬ LÝ ĐẶT HÀNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(FormCollection collection)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            List<CartItem> cart = GetCart();

            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            Customer currentCustomer = Session["Customer"] as Customer;
            string username = Session["Username"].ToString();

            if (currentCustomer == null)
            {
                currentCustomer = db.Customers
                    .FirstOrDefault(c => c.Username == username);
            }

            // Lấy địa chỉ giao hàng
            string addressDelivery = collection["AddressDelivery"];

            if (string.IsNullOrWhiteSpace(addressDelivery) &&
                currentCustomer != null)
            {
                addressDelivery = currentCustomer.CustomerAddress;
            }

            // 7. TẠO ĐƠN HÀNG MỚI
            Order order = new Order
            {
                CustomerID = currentCustomer != null
                    ? currentCustomer.CustomerID
                    : 0,

                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(i => i.TotalPrice),
                AddressDelivery = addressDelivery,

                OrderStatus = "Chờ xác nhận",
                PaymentStatus = "Chưa thanh toán"
            };

            db.Orders.Add(order);
            db.SaveChanges();

            // 8. TẠO CHI TIẾT ĐƠN HÀNG
            foreach (var item in cart)
            {
                OrderDetail detail = new OrderDetail
                {
                    OrderID = order.OrderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };

                db.OrderDetails.Add(detail);
            }

            db.SaveChanges();

            // 9. XÓA GIỎ HÀNG SAU KHI ĐẶT HÀNG THÀNH CÔNG
            Session["Cart"] = null;
            Session["CartCount"] = 0;

            if (UserCarts.ContainsKey(username))
            {
                UserCarts.Remove(username);
            }

            return View("OrderSuccess", order);
        }

        // Giải phóng kết nối cơ sở dữ liệu
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
