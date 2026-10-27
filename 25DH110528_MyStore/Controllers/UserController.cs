
using _25DH110528_MyStore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace _25DH110528_MyStore.Controllers
{
    public class UserController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // 1. HIEN TRANG DANG KY
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        // 2. XU LY DANG KY
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(User usr, Customer cus)
        {
            if (ModelState.IsValid)
            {
                var existingUser = db.Users
                    .FirstOrDefault(u => u.Username == usr.Username);

                if (existingUser != null)
                {
                    ModelState.AddModelError(
                        "Username",
                        "Tên đăng nhập này đã tồn tại!");

                    return View(cus);
                }

                usr.UserRole = "C";

                db.Configuration.ValidateOnSaveEnabled = false;

                try
                {
                    db.Users.Add(usr);
                    db.SaveChanges();

                    cus.Username = usr.Username;
                    db.Customers.Add(cus);
                    db.SaveChanges();
                }
                finally
                {
                    db.Configuration.ValidateOnSaveEnabled = true;
                }

                return RedirectToAction("Login");
            }

            return View(cus);
        }

        // 3. HIEN TRANG DANG NHAP
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // 4. XU LY DANG NHAP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User usr)
        {
            var userCheck = db.Users.FirstOrDefault(u =>
                u.Username == usr.Username &&
                u.Password == usr.Password);

            if (userCheck == null)
            {
                ModelState.AddModelError(
                    "",
                    "Tên đăng nhập hoặc mật khẩu không chính xác.");

                return View();
            }

            // Luu thong tin tai khoan vao Session
            Session["Username"] = userCheck.Username;
            Session["UserRole"] = userCheck.UserRole;

            var customerInfo = db.Customers.FirstOrDefault(c =>
                c.Username == userCheck.Username);

            if (customerInfo != null)
            {
                Session["CustomerID"] = customerInfo.CustomerID;
                Session["CustomerName"] = customerInfo.CustomerName;
                Session["Customer"] = customerInfo;
            }
            else
            {
                Session.Remove("CustomerID");
                Session.Remove("CustomerName");
                Session.Remove("Customer");
            }

            // Khoi phuc gio hang da luu cua tai khoan
            List<CartItem> savedCart =
                CartController.GetUserCart(userCheck.Username);

            if (savedCart != null && savedCart.Count > 0)
            {
                Session["Cart"] = savedCart;
                Session["CartCount"] =
                    savedCart.Sum(i => i.Quantity);
            }
            else
            {
                // Dong bo gio hang tam vao tai khoan neu co
                List<CartItem> currentCart =
                    Session["Cart"] as List<CartItem>;

                if (currentCart != null && currentCart.Count > 0)
                {
                    CartController.SyncUserCart(
                        userCheck.Username,
                        currentCart);

                    Session["CartCount"] =
                        currentCart.Sum(i => i.Quantity);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        // 5. DANG XUAT
        public ActionResult Logout()
        {
            // Luu gio hang truoc khi xoa Session
            string username = Session["Username"] as string;
            List<CartItem> cart =
                Session["Cart"] as List<CartItem>;

            if (!string.IsNullOrEmpty(username) && cart != null)
            {
                CartController.SyncUserCart(username, cart);
            }

            Session.Clear();

            return RedirectToAction("Index", "Home");
        }

        // 6. HIEN THONG TIN TAI KHOAN
        [HttpGet]
        [ActionName("Profile")]
        public ActionResult UserProfile()
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login");
            }

            string username = Session["Username"].ToString();

            var customer = db.Customers.FirstOrDefault(c =>
                c.Username == username);

            if (customer == null)
            {
                return HttpNotFound();
            }

            return View("Profile", customer);
        }

        // CAP NHAT THONG TIN TAI KHOAN
        [HttpPost]
        [ActionName("Profile")]
        [ValidateAntiForgeryToken]
        public ActionResult UserProfile(Customer model)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login");
            }

            string username = Session["Username"].ToString();

            // Tim khach hang tu tai khoan dang dang nhap,
            // khong tin CustomerID gui len tu form
            var customer = db.Customers.FirstOrDefault(c =>
                c.Username == username);

            if (customer == null)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                customer.CustomerName = model.CustomerName;
                customer.CustomerPhone = model.CustomerPhone;
                customer.CustomerEmail = model.CustomerEmail;
                customer.CustomerAddress = model.CustomerAddress;

                db.SaveChanges();

                Session["CustomerID"] = customer.CustomerID;
                Session["CustomerName"] = customer.CustomerName;
                Session["Customer"] = customer;

                ViewBag.SuccessMessage =
                    "Cập nhật thông tin thành công!";

                return View("Profile", customer);
            }

            return View("Profile", model);
        }

        // 7. LICH SU DON HANG
        [HttpGet]
        public ActionResult OrderHistory()
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login");
            }

            string username = Session["Username"].ToString();

            var customer = db.Customers.FirstOrDefault(c =>
                c.Username == username);

            if (customer == null)
            {
                return HttpNotFound();
            }

            // Chi lay don hang cua khach hang hien tai
            var orders = db.Orders
                .Where(o => o.CustomerID == customer.CustomerID)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // 8. CHI TIET DON HANG
        [HttpGet]
        public ActionResult OrderDetail(int id)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login");
            }

            string username = Session["Username"].ToString();

            var customer = db.Customers.FirstOrDefault(c =>
                c.Username == username);

            if (customer == null)
            {
                return HttpNotFound();
            }

            // Chi cho phep xem don hang cua tai khoan hien tai
            var order = db.Orders.FirstOrDefault(o =>
                o.OrderID == id &&
                o.CustomerID == customer.CustomerID);

            if (order == null)
            {
                return HttpNotFound();
            }

            var orderDetails = db.OrderDetails
                .Where(d => d.OrderID == order.OrderID)
                .ToList();

            ViewBag.Order = order;

            return View(orderDetails);
        }

        // Giai phong ket noi database
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
