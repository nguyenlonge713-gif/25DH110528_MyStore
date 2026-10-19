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

        // Bộ nhớ lưu trữ giỏ hàng tạm theo Username (Chống mất giỏ hàng khi Logout)
        private static Dictionary<string, List<CartItem>> UserCarts = new Dictionary<string, List<CartItem>>();

        // Hàm hỗ trợ đồng bộ giỏ hàng theo Username
        public static void SyncUserCart(string username, List<CartItem> cart)
        {
            if (!string.IsNullOrEmpty(username))
            {
                UserCarts[username] = cart;
            }
        }

        // Hàm hỗ trợ lấy giỏ hàng đã lưu của User
        public static List<CartItem> GetUserCart(string username)
        {
            if (!string.IsNullOrEmpty(username) && UserCarts.ContainsKey(username))
            {
                return UserCarts[username];
            }
            return null;
        }

        // Lấy giỏ hàng từ Session hoặc bộ nhớ đệm của User
        private List<CartItem> GetCart()
        {
            List<CartItem> cart = Session["Cart"] as List<CartItem>;
            string username = Session["Username"] as string;

            if (cart == null)
            {
                // Thử khôi phục từ bộ nhớ đệm nếu user đã đăng nhập
                if (!string.IsNullOrEmpty(username) && UserCarts.ContainsKey(username))
                {
                    cart = UserCarts[username];
                }
                else
                {
                    cart = new List<CartItem>();
                }
                Session["Cart"] = cart;
            }

            // Đồng bộ ngược lại bộ nhớ nếu đang đăng nhập
            if (!string.IsNullOrEmpty(username))
            {
                UserCarts[username] = cart;
            }

            Session["CartCount"] = cart.Sum(item => item.Quantity);
            return cart;
        }

        // 1. TRANG XEM GIỎ HÀNG
        public ActionResult Index()
        {
            List<CartItem> cart = GetCart();
            ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);
            return View(cart);
        }

        // 2. THÊM SẢN PHẨM VÀO GIỎ HÀNG
        public ActionResult AddToCart(int id, int quantity = 1)
        {
            var product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
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

            // Lưu và đồng bộ
            Session["Cart"] = cart;
            string username = Session["Username"] as string;
            if (!string.IsNullOrEmpty(username))
            {
                UserCarts[username] = cart;
            }

            GetCart();
            return RedirectToAction("Index");
        }

        // 3. CẬP NHẬT SỐ LƯỢNG SẢN PHẨM TRONG GIỎ
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

            Session["Cart"] = cart;
            string username = Session["Username"] as string;
            if (!string.IsNullOrEmpty(username))
            {
                UserCarts[username] = cart;
            }

            GetCart();
            return RedirectToAction("Index");
        }

        // 4. XÓA MỘT SẢN PHẨM KHỎI GIỎ
        public ActionResult RemoveFromCart(int id)
        {
            List<CartItem> cart = GetCart();
            CartItem item = cart.FirstOrDefault(p => p.ProductID == id);

            if (item != null)
            {
                cart.Remove(item);
            }

            Session["Cart"] = cart;
            string username = Session["Username"] as string;
            if (!string.IsNullOrEmpty(username))
            {
                UserCarts[username] = cart;
            }

            GetCart();
            return RedirectToAction("Index");
        }

        // 5. TRANG THANH TOÁN (CHECKOUT)
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

        // 6. XỬ LÝ ĐẶT HÀNG (LƯU VÀO CSDL: ORDER & ORDERDETAIL)
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
                currentCustomer = db.Customers.FirstOrDefault(c => c.Username == username);
            }

            string addressDelivery = collection["AddressDelivery"];
            if (string.IsNullOrWhiteSpace(addressDelivery) && currentCustomer != null)
            {
                addressDelivery = currentCustomer.CustomerAddress;
            }

            // 1. Tạo Đơn hàng mới (Order)
            Order order = new Order
            {
                CustomerID = currentCustomer != null ? currentCustomer.CustomerID : 0,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(i => i.TotalPrice),
                AddressDelivery = addressDelivery
            };

            db.Orders.Add(order);
            db.SaveChanges();

            // 2. Tạo Chi tiết đơn hàng (OrderDetail)
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

            // 3. Xóa giỏ hàng sau khi đặt thành công
            Session["Cart"] = null;
            Session["CartCount"] = 0;
            if (UserCarts.ContainsKey(username))
            {
                UserCarts.Remove(username); // Xóa giỏ hàng đã thanh toán khỏi bộ nhớ
            }

            return View("OrderSuccess", order);
        }
    }
}