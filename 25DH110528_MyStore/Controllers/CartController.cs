
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.Entity;
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

            // Kiểm tra sản phẩm còn hàng không
            if (product.StockQuantity <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Sản phẩm hiện đã hết hàng."
                });
            }

            List<CartItem> cart = GetCart();

            CartItem item = cart.FirstOrDefault(p => p.ProductID == id);

            // Tính tổng số lượng sau khi thêm
            int currentQuantity = item != null ? item.Quantity : 0;
            int newQuantity = currentQuantity + quantity;

            // Không cho phép vượt quá tồn kho
            if (newQuantity > product.StockQuantity)
            {
                return Json(new
                {
                    success = false,
                    message = "Số lượng vượt quá tồn kho. Hiện chỉ còn "
                        + product.StockQuantity + " sản phẩm."
                });
            }

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
                item.Quantity = newQuantity;
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
                    var product = db.Products.Find(id);

                    if (product == null)
                    {
                        TempData["CartError"] =
                            "Sản phẩm không còn tồn tại.";

                        return RedirectToAction("Index");
                    }

                    // Kiểm tra số lượng cập nhật có vượt tồn kho không
                    if (quantity > product.StockQuantity)
                    {
                        TempData["CartError"] =
                            "Số lượng yêu cầu vượt quá tồn kho. Hiện chỉ còn "
                            + product.StockQuantity + " sản phẩm.";

                        return RedirectToAction("Index");
                    }

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

            // Kiểm tra tồn kho khi mở trang thanh toán
            foreach (var item in cart)
            {
                var product = db.Products.Find(item.ProductID);

                if (product == null || product.StockQuantity < item.Quantity)
                {
                    ViewBag.StockError =
                        "Sản phẩm " + item.ProductName
                        + " không còn đủ số lượng trong kho. "
                        + "Vui lòng quay lại giỏ hàng để cập nhật.";

                    ViewBag.TotalAmount =
                        cart.Sum(i => i.TotalPrice);

                    return View(cart);
                }
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

            string username = Session["Username"].ToString();

            Customer currentCustomer = Session["Customer"] as Customer;

            if (currentCustomer == null)
            {
                currentCustomer = db.Customers
                    .FirstOrDefault(c => c.Username == username);
            }

            if (currentCustomer == null)
            {
                ViewBag.StockError =
                    "Không tìm thấy thông tin khách hàng.";

                ViewBag.TotalAmount =
                    cart.Sum(i => i.TotalPrice);

                return View("Checkout", cart);
            }

            // Lấy địa chỉ giao hàng
            string addressDelivery = collection["AddressDelivery"];

            if (string.IsNullOrWhiteSpace(addressDelivery))
            {
                addressDelivery = currentCustomer.CustomerAddress;
            }

            if (string.IsNullOrWhiteSpace(addressDelivery))
            {
                ViewBag.StockError =
                    "Vui lòng nhập địa chỉ giao hàng.";

                ViewBag.TotalAmount =
                    cart.Sum(i => i.TotalPrice);

                return View("Checkout", cart);
            }

            using (var transaction =
                db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    // Lấy danh sách sản phẩm mới nhất từ database
                    List<int> productIds = cart
                        .Select(i => i.ProductID)
                        .Distinct()
                        .ToList();

                    var products = db.Products
                        .Where(p => productIds.Contains(p.ProductID))
                        .ToList();

                    // Kiểm tra từng sản phẩm trước khi đặt hàng
                    foreach (var item in cart)
                    {
                        var product = products.FirstOrDefault(
                            p => p.ProductID == item.ProductID);

                        if (product == null)
                        {
                            transaction.Rollback();

                            ViewBag.StockError =
                                "Sản phẩm " + item.ProductName
                                + " không còn tồn tại.";

                            ViewBag.TotalAmount =
                                cart.Sum(i => i.TotalPrice);

                            return View("Checkout", cart);
                        }

                        if (product.StockQuantity < item.Quantity)
                        {
                            transaction.Rollback();

                            ViewBag.StockError =
                                "Sản phẩm " + product.ProductName
                                + " không đủ số lượng trong kho. "
                                + "Số lượng hiện còn: "
                                + product.StockQuantity + ".";

                            ViewBag.TotalAmount =
                                cart.Sum(i => i.TotalPrice);

                            return View("Checkout", cart);
                        }

                        // Cập nhật thông tin mới nhất vào giỏ hàng
                        item.ProductName = product.ProductName;
                        item.ProductImage = product.ProductImage;
                        item.UnitPrice =
                            Convert.ToDecimal(product.ProductPrice);
                    }

                    // Tính lại tổng tiền theo giá mới nhất
                    decimal totalAmount =
                        cart.Sum(i => i.TotalPrice);

                    // 7. TẠO ĐƠN HÀNG MỚI
                    Order order = new Order
                    {
                        CustomerID = currentCustomer.CustomerID,
                        OrderDate = DateTime.Now,
                        TotalAmount = totalAmount,
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

                    // Lưu đơn hàng và chi tiết đơn hàng
                    transaction.Commit();

                    // 9. XÓA GIỎ HÀNG SAU KHI ĐẶT HÀNG THÀNH CÔNG
                    Session["Cart"] = null;
                    Session["CartCount"] = 0;

                    if (UserCarts.ContainsKey(username))
                    {
                        UserCarts.Remove(username);
                    }

                    return View("OrderSuccess", order);
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    ViewBag.StockError =
                        "Đã xảy ra lỗi khi đặt hàng. "
                        + "Vui lòng thử lại.";

                    ViewBag.TotalAmount =
                        cart.Sum(i => i.TotalPrice);

                    return View("Checkout", cart);
                }
            }
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
