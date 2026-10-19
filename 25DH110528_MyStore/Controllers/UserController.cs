using _25DH110528_MyStore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace _25DH110528_MyStore.Controllers
{
    public class UserController : Controller
    {
        MyStoreEntities db = new MyStoreEntities();

        // 1. HIỆN TRANG ĐĂNG KÝ
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        // 2. XỬ LÝ ĐĂNG KÝ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(User usr, Customer cus)
        {
            if (ModelState.IsValid)
            {
                var existingUser = db.Users.FirstOrDefault(u => u.Username == usr.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại!");
                    return View(cus);
                }

                usr.UserRole = "C";
                db.Configuration.ValidateOnSaveEnabled = false;

                db.Users.Add(usr);
                db.SaveChanges();

                cus.Username = usr.Username;
                db.Customers.Add(cus);
                db.SaveChanges();

                db.Configuration.ValidateOnSaveEnabled = true;

                return RedirectToAction("Login");
            }
            return View(cus);
        }

        // 3. HIỆN TRANG ĐĂNG NHẬP
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // 4. XỬ LÝ ĐĂNG NHẬP (TỰ ĐỘNG KHÔI PHỤC GIỎ HÀNG CỦA TÀI KHOẢN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User usr)
        {
            var userCheck = db.Users.FirstOrDefault(u => u.Username == usr.Username && u.Password == usr.Password);

            if (userCheck != null)
            {
                Session["Username"] = userCheck.Username;
                Session["UserRole"] = userCheck.UserRole;

                var customerInfo = db.Customers.FirstOrDefault(c => c.Username == userCheck.Username);
                if (customerInfo != null)
                {
                    Session["CustomerName"] = customerInfo.CustomerName;
                    Session["Customer"] = customerInfo;
                }

                // KHÔI PHỤC GIỎ HÀNG CỦA TÀI KHOẢN NÀY NẾU CÓ
                List<CartItem> savedCart = CartController.GetUserCart(userCheck.Username);
                if (savedCart != null && savedCart.Count > 0)
                {
                    Session["Cart"] = savedCart;
                    Session["CartCount"] = savedCart.Sum(i => i.Quantity);
                }
                else
                {
                    // Nếu trước khi đăng nhập đã có sản phẩm trong giỏ tạm, lưu luôn vào tài khoản
                    List<CartItem> currentCart = Session["Cart"] as List<CartItem>;
                    if (currentCart != null && currentCart.Count > 0)
                    {
                        CartController.SyncUserCart(userCheck.Username, currentCart);
                        Session["CartCount"] = currentCart.Sum(i => i.Quantity);
                    }
                }

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
            }
            return View();
        }

        // 5. ĐĂNG XUẤT
        public ActionResult Logout()
        {
            // Trước khi xóa Session, lưu lại trạng thái giỏ hàng vào tài khoản
            string username = Session["Username"] as string;
            List<CartItem> cart = Session["Cart"] as List<CartItem>;
            if (!string.IsNullOrEmpty(username) && cart != null)
            {
                CartController.SyncUserCart(username, cart);
            }

            Session.Clear(); // Xóa toàn bộ Session phiên hiện tại
            return RedirectToAction("Index", "Home");
        }

        // 6. THÔNG TIN TÀI KHOẢN
        [HttpGet]
        [ActionName("Profile")]
        public ActionResult UserProfile()
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login");
            }

            string username = Session["Username"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.Username == username);

            if (customer == null)
            {
                return HttpNotFound();
            }

            return View("Profile", customer);
        }

        // CẬP NHẬT THÔNG TIN TÀI KHOẢN
        [HttpPost]
        [ActionName("Profile")]
        [ValidateAntiForgeryToken]
        public ActionResult UserProfile(Customer model)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                var customer = db.Customers.Find(model.CustomerID);
                if (customer != null)
                {
                    customer.CustomerName = model.CustomerName;
                    customer.CustomerPhone = model.CustomerPhone;
                    customer.CustomerEmail = model.CustomerEmail;
                    customer.CustomerAddress = model.CustomerAddress;

                    db.SaveChanges();

                    Session["CustomerName"] = customer.CustomerName;
                    Session["Customer"] = customer;

                    ViewBag.SuccessMessage = "Cập nhật thông tin thành công!";
                }
            }

            return View("Profile", model);
        }

        // 7. LỊCH SỬ MUA HÀNG
        [HttpGet]
        public ActionResult OrderHistory()
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login");
            }

            string username = Session["Username"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.Username == username);

            if (customer != null)
            {
                var orders = db.Orders.Where(o => o.CustomerID == customer.CustomerID)
                                      .OrderByDescending(o => o.OrderDate)
                                      .ToList();
                return View(orders);
            }

            return View(new List<Order>());
        }

        // 8. CHI TIẾT ĐƠN HÀNG TRONG LỊCH SỬ
        [HttpGet]
        public ActionResult OrderDetail(int id)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login");
            }

            var order = db.Orders.FirstOrDefault(o => o.OrderID == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            var orderDetails = db.OrderDetails.Where(d => d.OrderID == id).ToList();
            ViewBag.Order = order;

            return View(orderDetails);
        }
    }
}