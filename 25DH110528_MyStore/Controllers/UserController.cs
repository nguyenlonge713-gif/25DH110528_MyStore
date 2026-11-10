
using _25DH110528_MyStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace _25DH110528_MyStore.Controllers
{
    public class UserController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // ==========================================
        // CAC HAM KIEM TRA DU LIEU
        // ==========================================

        // Kiem tra username: 4-20 ky tu, khong dau
        private bool IsValidUsername(string username)
        {
            return !string.IsNullOrWhiteSpace(username)
                && Regex.IsMatch(username, @"^[A-Za-z0-9._]{4,20}$");
        }

        // Kiem tra mat khau: 8-30 ky tu
        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (password.Length < 8 || password.Length > 30)
                return false;

            bool hasUpper = Regex.IsMatch(password, @"[A-Z]");
            bool hasLower = Regex.IsMatch(password, @"[a-z]");
            bool hasNumber = Regex.IsMatch(password, @"[0-9]");
            bool hasSpecial = Regex.IsMatch(password, @"[^A-Za-z0-9]");

            return hasUpper && hasLower && hasNumber && hasSpecial;
        }

        // Kiem tra ho ten
        private bool IsValidFullName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            name = name.Trim();

            if (name.Length < 2 || name.Length > 60)
                return false;

            return name.All(c =>
                char.IsLetter(c) ||
                char.IsWhiteSpace(c) ||
                c == '-' ||
                c == '\'' ||
                c == '’');
        }

        // Kiem tra so di dong Viet Nam
        private bool IsValidVietnamesePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            phone = phone.Trim();

            return Regex.IsMatch(
                phone,
                @"^(032|033|034|035|036|037|038|039|" +
                @"052|055|056|058|059|" +
                @"070|076|077|078|079|" +
                @"081|082|083|084|085|086|087|088|089|" +
                @"090|091|092|093|094|096|097|098|099)\d{7}$");
        }

        // Kiem tra email
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim();

            try
            {
                MailAddress address = new MailAddress(email);

                return address.Address.Equals(
                    email,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        // ==========================================
        // 1. HIEN TRANG DANG KY
        // ==========================================

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        // ==========================================
        // 2. XU LY DANG KY
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(
            User usr,
            Customer cus,
            string ConfirmPassword)
        {
            if (usr == null)
                usr = new User();

            if (cus == null)
                cus = new Customer();

            string username = (usr.Username ?? "").Trim();
            string fullName = (cus.CustomerName ?? "").Trim();
            string phone = (cus.CustomerPhone ?? "").Trim();
            string email = (cus.CustomerEmail ?? "").Trim();
            string address = (cus.CustomerAddress ?? "").Trim();

            ViewBag.RegisterUsername = username;

            // Kiem tra username
            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Vui long nhap ten dang nhap.");
            }
            else if (!IsValidUsername(username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Username phai tu 4-20 ky tu, chi gom chu khong dau, so, dau cham va dau gach duoi.");
            }
            else if (db.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Ten dang nhap nay da ton tai.");
            }

            // Kiem tra mat khau
            if (string.IsNullOrEmpty(usr.Password))
            {
                ModelState.AddModelError(
                    "Password",
                    "Vui long nhap mat khau.");
            }
            else if (!IsValidPassword(usr.Password))
            {
                ModelState.AddModelError(
                    "Password",
                    "Mat khau phai co 8-30 ky tu, bao gom chu hoa, chu thuong, chu so va ky tu dac biet.");
            }

            // Kiem tra xac nhan mat khau
            if (string.IsNullOrEmpty(ConfirmPassword))
            {
                ModelState.AddModelError(
                    "ConfirmPassword",
                    "Vui long nhap lai mat khau.");
            }
            else if (usr.Password != ConfirmPassword)
            {
                ModelState.AddModelError(
                    "ConfirmPassword",
                    "Mat khau xac nhan khong trung khop.");
            }

            // Kiem tra ho ten
            if (!IsValidFullName(fullName))
            {
                ModelState.AddModelError(
                    "CustomerName",
                    "Ho ten phai tu 2-60 ky tu va khong duoc chua so hoac ky tu dac biet khong hop le.");
            }

            // Kiem tra so dien thoai
            if (string.IsNullOrWhiteSpace(phone))
            {
                ModelState.AddModelError(
                    "CustomerPhone",
                    "Vui long nhap so dien thoai.");
            }
            else if (!IsValidVietnamesePhone(phone))
            {
                ModelState.AddModelError(
                    "CustomerPhone",
                    "So dien thoai phai gom 10 chu so va la dau so di dong Viet Nam hop le.");
            }
            else if (db.Customers.Any(c => c.CustomerPhone == phone))
            {
                ModelState.AddModelError(
                    "CustomerPhone",
                    "So dien thoai nay da duoc su dung.");
            }

            // Kiem tra email
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "CustomerEmail",
                    "Vui long nhap email.");
            }
            else if (!IsValidEmail(email))
            {
                ModelState.AddModelError(
                    "CustomerEmail",
                    "Dia chi email khong dung dinh dang.");
            }
            else if (db.Customers.Any(c =>
                c.CustomerEmail.ToLower() == email.ToLower()))
            {
                ModelState.AddModelError(
                    "CustomerEmail",
                    "Email nay da duoc su dung.");
            }

            // Neu co loi thi quay lai trang dang ky
            if (!ModelState.IsValid)
            {
                return View(cus);
            }

            // Chuan hoa du lieu
            usr.Username = username;

            // Khong cho phep nguoi dung tu dang ky tai khoan Admin
            usr.UserRole = "C";

            cus.CustomerName = fullName;
            cus.CustomerPhone = phone;
            cus.CustomerEmail = email;
            cus.CustomerAddress = address;
            cus.Username = username;

            // Luu tai khoan va khach hang trong cung transaction
            using (var transaction = db.Database.BeginTransaction())
            {
                bool oldValidationSetting =
                    db.Configuration.ValidateOnSaveEnabled;

                try
                {
                    db.Configuration.ValidateOnSaveEnabled = false;

                    // Them tai khoan
                    db.Users.Add(usr);
                    db.SaveChanges();

                    // Them thong tin khach hang
                    db.Customers.Add(cus);
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["RegisterSuccess"] =
                        "Dang ky tai khoan thanh cong! Vui long dang nhap.";

                    return RedirectToAction("Login", "User");
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    ModelState.AddModelError(
                        "",
                        "Dang ky that bai. Vui long kiem tra lai thong tin hoac thu lai sau.");

                    return View(cus);
                }
                finally
                {
                    db.Configuration.ValidateOnSaveEnabled =
                        oldValidationSetting;
                }
            }
        }

        // ==========================================
        // 3. HIEN TRANG DANG NHAP
        // ==========================================

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // ==========================================
        // 4. XU LY DANG NHAP
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User usr)
        {
            if (usr == null ||
                string.IsNullOrWhiteSpace(usr.Username) ||
                string.IsNullOrEmpty(usr.Password))
            {
                ModelState.AddModelError(
                    "",
                    "Vui long nhap day du ten dang nhap va mat khau.");

                return View();
            }

            string username = usr.Username.Trim();

            var userCheck = db.Users.FirstOrDefault(u =>
                u.Username == username &&
                u.Password == usr.Password);

            if (userCheck == null)
            {
                ModelState.AddModelError(
                    "",
                    "Ten dang nhap hoac mat khau khong chinh xac.");

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

            // Khoi phuc gio hang da luu
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

            // Neu la Admin thi chuyen den trang quan ly san pham
            if (string.Equals(
                (userCheck.UserRole ?? "").Trim(),
                "A",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Index",
                    "Products",
                    new { area = "Admin" });
            }

            // Neu la khach hang thi ve trang chu
            return RedirectToAction(
                "Index",
                "Home",
                new { area = "" });
        }

        // ==========================================
        // 5. DANG XUAT
        // ==========================================

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

        // ==========================================
        // 6. HIEN THONG TIN TAI KHOAN
        // ==========================================

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

        // ==========================================
        // CAP NHAT THONG TIN TAI KHOAN
        // ==========================================

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

            // Tim khach hang tu tai khoan dang dang nhap
            var customer = db.Customers.FirstOrDefault(c =>
                c.Username == username);

            if (customer == null)
            {
                return HttpNotFound();
            }

            if (model == null)
            {
                ModelState.AddModelError(
                    "",
                    "Du lieu cap nhat khong hop le.");

                return View("Profile", customer);
            }

            string fullName = (model.CustomerName ?? "").Trim();
            string phone = (model.CustomerPhone ?? "").Trim();
            string email = (model.CustomerEmail ?? "").Trim();
            string address = (model.CustomerAddress ?? "").Trim();

            // Kiem tra ho ten
            if (!IsValidFullName(fullName))
            {
                ModelState.AddModelError(
                    "CustomerName",
                    "Ho ten phai tu 2-60 ky tu va khong duoc chua so.");
            }

            // Kiem tra so dien thoai
            if (!IsValidVietnamesePhone(phone))
            {
                ModelState.AddModelError(
                    "CustomerPhone",
                    "So dien thoai phai gom 10 chu so va la dau so di dong Viet Nam hop le.");
            }
            else if (db.Customers.Any(c =>
                c.CustomerPhone == phone &&
                c.CustomerID != customer.CustomerID))
            {
                ModelState.AddModelError(
                    "CustomerPhone",
                    "So dien thoai nay da duoc tai khoan khac su dung.");
            }

            // Kiem tra email
            if (!IsValidEmail(email))
            {
                ModelState.AddModelError(
                    "CustomerEmail",
                    "Dia chi email khong dung dinh dang.");
            }
            else if (db.Customers.Any(c =>
                c.CustomerEmail.ToLower() == email.ToLower() &&
                c.CustomerID != customer.CustomerID))
            {
                ModelState.AddModelError(
                    "CustomerEmail",
                    "Email nay da duoc tai khoan khac su dung.");
            }

            if (!ModelState.IsValid)
            {
                model.CustomerName = fullName;
                model.CustomerPhone = phone;
                model.CustomerEmail = email;
                model.CustomerAddress = address;

                return View("Profile", model);
            }

            customer.CustomerName = fullName;
            customer.CustomerPhone = phone;
            customer.CustomerEmail = email;
            customer.CustomerAddress = address;

            db.SaveChanges();

            Session["CustomerID"] = customer.CustomerID;
            Session["CustomerName"] = customer.CustomerName;
            Session["Customer"] = customer;

            ViewBag.SuccessMessage =
                "Cap nhat thong tin thanh cong!";

            return View("Profile", customer);
        }

        // ==========================================
        // 7. LICH SU DON HANG
        // ==========================================

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

            var orders = db.Orders
                .Where(o => o.CustomerID == customer.CustomerID)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // ==========================================
        // 8. CHI TIET DON HANG
        // ==========================================

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

        // ==========================================
        // GIAI PHONG KET NOI DATABASE
        // ==========================================

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
