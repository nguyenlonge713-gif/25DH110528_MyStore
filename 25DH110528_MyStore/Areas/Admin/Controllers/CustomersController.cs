using System;
using System.Linq;
using System.Data.Entity;
using System.Net;
using System.Web.Mvc;
using _25DH110528_MyStore.Models;

namespace _25DH110528_MyStore.Areas.Admin.Controllers
{
    public class CustomersController : AdminBaseController
    {
        private MyStoreEntities db = new MyStoreEntities();
        // Kiem tra so dien thoai Viet Nam
        private bool IsValidVietnamesePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            phone = phone.Trim();

            // Phai gom dung 10 chu so
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                phone, @"^0[0-9]{9}$"))
                return false;

            // Khong cho phep 10 chu so giong nhau
            if (System.Text.RegularExpressions.Regex.IsMatch(
                phone, @"^([0-9])\1{9}$"))
                return false;

            // Kiem tra dau so di dong Viet Nam
            return System.Text.RegularExpressions.Regex.IsMatch(
                phone,
                @"^(032|033|034|035|036|037|038|039|" +
                @"052|055|056|058|059|" +
                @"070|076|077|078|079|" +
                @"081|082|083|084|085|086|087|088|089|" +
                @"090|091|092|093|094|096|097|098|099)\d{7}$");
        }
        // GET: Admin/Customers
        // DANH SACH VA TIM KIEM KHACH HANG
        public ActionResult Index(string searchString)
        {
            var customers = db.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                customers = customers.Where(c =>
                    (c.CustomerName != null &&
                     c.CustomerName.Contains(searchString)) ||

                    (c.CustomerPhone != null &&
                     c.CustomerPhone.Contains(searchString)) ||

                    (c.CustomerEmail != null &&
                     c.CustomerEmail.Contains(searchString)) ||

                    (c.Username != null &&
                     c.Username.Contains(searchString)));
            }

            ViewBag.SearchString = searchString;

            return View(customers
                .OrderByDescending(c => c.CustomerID)
                .ToList());
        }

        // GET: Admin/Customers/Details/5
        // XEM CHI TIET KHACH HANG
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            Customer customer = db.Customers
                .Include(c => c.Orders)
                .FirstOrDefault(c => c.CustomerID == id);

            if (customer == null)
            {
                return HttpNotFound();
            }

            return View(customer);
        }

        // GET: Admin/Customers/Create
        // HIEN FORM THEM KHACH HANG
        public ActionResult Create()
        {
            return View(new Customer());
        }

        // POST: Admin/Customers/Create
        // TAO DONG THOI USER VA CUSTOMER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            Customer customer,
            string Username,
            string Password,
            string ConfirmPassword)
        {
            Username = (Username ?? "").Trim();

            if (string.IsNullOrWhiteSpace(customer.CustomerName))
            {
                ModelState.AddModelError(
                    "CustomerName",
                    "Vui lòng nhập tên khách hàng.");
            }

            if (string.IsNullOrWhiteSpace(Username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Vui lòng nhập tên đăng nhập.");
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(
                    "Password",
                    "Vui lòng nhập mật khẩu.");
            }

            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError(
                    "ConfirmPassword",
                    "Mật khẩu xác nhận không khớp.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Username = Username;
                return View(customer);
            }

            bool usernameExists = db.Users.Any(
                u => u.Username == Username);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập này đã tồn tại.");

                ViewBag.Username = Username;
                return View(customer);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    User user = new User
                    {
                        Username = Username,
                        Password = Password,
                        UserRole = "C"
                    };

                    customer.Username = Username;
                    customer.CustomerName =
                        customer.CustomerName.Trim();

                    // Luu User truoc
                    db.Users.Add(user);
                    db.SaveChanges();

                    // Sau do luu Customer
                    db.Customers.Add(customer);
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["SuccessMessage"] =
                        "Thêm khách hàng và tài khoản thành công!";

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    ModelState.AddModelError(
                        "",
                        "Không thể thêm khách hàng: " +
                        ex.GetBaseException().Message);

                    ViewBag.Username = Username;
                    return View(customer);
                }
            }
        }

        // GET: Admin/Customers/Edit/5
        // HIEN FORM CHINH SUA
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            Customer customer = db.Customers.Find(id);

            if (customer == null)
            {
                return HttpNotFound();
            }

            return View(customer);
        }

        // POST: Admin/Customers/Edit/5
        // CAP NHAT THONG TIN KHACH HANG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            [Bind(Include =
        "CustomerID,CustomerName,CustomerPhone,CustomerEmail,CustomerAddress")]
    Customer model)
        {
            if (model == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            Customer customer = db.Customers.Find(model.CustomerID);

            if (customer == null)
            {
                return HttpNotFound();
            }

            string fullName = (model.CustomerName ?? "").Trim();
            string phone = (model.CustomerPhone ?? "").Trim();
            string email = (model.CustomerEmail ?? "").Trim();

            // Kiem tra ho ten
            if (string.IsNullOrWhiteSpace(fullName))
            {
                ModelState.AddModelError(
                    "CustomerName",
                    "Vui lòng nhập tên khách hàng.");
            }

            // Kiem tra so dien thoai
            if (!IsValidVietnamesePhone(phone))
            {
                ModelState.AddModelError(
                    "CustomerPhone",
                    "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0, thuộc đầu số di động Việt Nam hợp lệ và không được trùng toàn bộ các chữ số.");
            }
            else if (db.Customers.Any(c =>
                c.CustomerPhone == phone &&
                c.CustomerID != customer.CustomerID))
            {
                ModelState.AddModelError(
                    "CustomerPhone",
                    "Số điện thoại này đã được khách hàng khác sử dụng.");
            }

            // Kiem tra email
            if (!string.IsNullOrWhiteSpace(email))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(
                    email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ModelState.AddModelError(
                        "CustomerEmail",
                        "Địa chỉ email không đúng định dạng.");
                }
                else if (db.Customers.Any(c =>
                    c.CustomerEmail.ToLower() == email.ToLower() &&
                    c.CustomerID != customer.CustomerID))
                {
                    ModelState.AddModelError(
                        "CustomerEmail",
                        "Email này đã được khách hàng khác sử dụng.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.CustomerName = fullName;
                model.CustomerPhone = phone;
                model.CustomerEmail = email;
                return View(model);
            }

            try
            {
                customer.CustomerName = fullName;
                customer.CustomerPhone = phone;
                customer.CustomerEmail = email;
                customer.CustomerAddress =
                    (model.CustomerAddress ?? "").Trim();

                // Khong thay doi Username va User
                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Cập nhật thông tin khách hàng thành công!";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật thông tin khách hàng: " +
                    ex.GetBaseException().Message);

                return View(model);
            }
        }

        // GET: Admin/Customers/Delete/5
        // HIEN THONG TIN TRUOC KHI XOA
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            Customer customer = db.Customers
                .Include(c => c.Orders)
                .FirstOrDefault(c => c.CustomerID == id);

            if (customer == null)
            {
                return HttpNotFound();
            }

            ViewBag.OrderCount = customer.Orders.Count;

            return View(customer);
        }

        // POST: Admin/Customers/Delete/5
        // XOA CUSTOMER VA USER LIEN KET
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Tim khach hang
            Customer customer = db.Customers
                .FirstOrDefault(c => c.CustomerID == id);

            if (customer == null)
            {
                TempData["DeleteError"] =
                    "Không tìm thấy khách hàng cần xóa.";

                return RedirectToAction("Index");
            }

            // Kiem tra don hang
            bool hasOrders = db.Orders.Any(
                o => o.CustomerID == id);

            if (hasOrders)
            {
                TempData["DeleteError"] =
                    "Không thể xóa khách hàng vì khách hàng đã có đơn hàng.";

                return RedirectToAction("Delete", new { id = id });
            }

            // Tim tai khoan User lien ket
            User user = null;

            if (!string.IsNullOrWhiteSpace(customer.Username))
            {
                user = db.Users.FirstOrDefault(
                    u => u.Username == customer.Username);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Buoc 1: Xoa Customer truoc
                    db.Customers.Remove(customer);
                    db.SaveChanges();

                    // Buoc 2: Xoa User sau
                    if (user != null)
                    {
                        db.Users.Remove(user);
                        db.SaveChanges();
                    }

                    // Buoc 3: Hoan tat transaction
                    transaction.Commit();

                    TempData["SuccessMessage"] =
                        "Xóa khách hàng và tài khoản đăng nhập thành công!";

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    TempData["DeleteError"] =
                        "Không thể xóa khách hàng: " +
                        ex.GetBaseException().Message;

                    return RedirectToAction(
                        "Delete",
                        new { id = id });
                }
            }
        }

        // GIAI PHONG DBCONTEXT
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