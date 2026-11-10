
using _25DH110528_MyStore.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace _25DH110528_MyStore.Areas.Admin.Controllers
{
    public class UsersController : AdminBaseController
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Admin/Users
        public ActionResult Index(string search, string role)
        {
            var users = db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                users = users.Where(u => u.Username.Contains(search));
            }

            if (role == "A" || role == "C")
            {
                users = users.Where(u => u.UserRole == role);
            }

            ViewBag.Search = search;
            ViewBag.Role = role;

            return View(users.OrderBy(u => u.Username).ToList());
        }

        // GET: Admin/Users/EditRole
        public ActionResult EditRole(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return RedirectToAction("Index");

            var user = db.Users.FirstOrDefault(
                u => u.Username == username);

            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // POST: Admin/Users/EditRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRole(string username, string userRole)
        {
            if (string.IsNullOrWhiteSpace(username))
                return RedirectToAction("Index");

            if (userRole != "A" && userRole != "C")
            {
                TempData["Error"] = "Vai trò không hợp lệ.";
                return RedirectToAction("EditRole",
                    new { username = username });
            }

            var user = db.Users.FirstOrDefault(
                u => u.Username == username);

            if (user == null)
                return HttpNotFound();

            string currentUsername =
                (Session["Username"] ?? "").ToString();

            // Không cho Admin tự thay đổi quyền của chính mình
            if (string.Equals(
                currentUsername,
                user.Username,
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Bạn không thể tự thay đổi quyền tài khoản của mình.";

                return RedirectToAction("EditRole",
                    new { username = username });
            }

            // Không cho hạ quyền Admin cuối cùng
            if (user.UserRole.Trim() == "A" && userRole == "C")
            {
                int adminCount = db.Users.Count(
                    u => u.UserRole == "A");

                if (adminCount <= 1)
                {
                    TempData["Error"] =
                        "Không thể hạ quyền Admin cuối cùng.";

                    return RedirectToAction("EditRole",
                        new { username = username });
                }
            }

            user.UserRole = userRole;
            db.SaveChanges();

            TempData["Success"] =
                "Cập nhật quyền tài khoản thành công.";

            return RedirectToAction("Index");
        }

        // GET: Admin/Users/ResetPassword
        public ActionResult ResetPassword(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return RedirectToAction("Index");

            var user = db.Users.FirstOrDefault(
                u => u.Username == username);

            if (user == null)
                return HttpNotFound();

            string currentUsername =
                (Session["Username"] ?? "").ToString();

            // Không cho Admin tự đặt lại mật khẩu tại đây
            if (string.Equals(
                currentUsername,
                user.Username,
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Bạn không thể đặt lại mật khẩu của chính mình tại đây.";

                return RedirectToAction("Index");
            }

            ViewBag.Username = user.Username;

            return View();
        }

        // POST: Admin/Users/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(
            string username,
            string newPassword,
            string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(username))
                return RedirectToAction("Index");

            var user = db.Users.FirstOrDefault(
                u => u.Username == username);

            if (user == null)
                return HttpNotFound();

            string currentUsername =
                (Session["Username"] ?? "").ToString();

            if (string.Equals(
                currentUsername,
                user.Username,
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Bạn không thể đặt lại mật khẩu của chính mình tại đây.";

                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(newPassword) ||
                newPassword.Length < 8 ||
                newPassword.Length > 30 ||
                !Regex.IsMatch(newPassword, "[A-Z]") ||
                !Regex.IsMatch(newPassword, "[a-z]") ||
                !Regex.IsMatch(newPassword, "[0-9]") ||
                !Regex.IsMatch(newPassword, "[^a-zA-Z0-9]"))
            {
                ViewBag.Username = user.Username;
                ViewBag.Error =
                    "Mật khẩu phải có 8-30 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.";

                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Username = user.Username;
                ViewBag.Error =
                    "Mật khẩu xác nhận không khớp.";

                return View();
            }

            // Giữ tương thích với cơ chế đăng nhập hiện tại
            user.Password = newPassword;

            db.SaveChanges();

            TempData["Success"] =
                "Đặt lại mật khẩu thành công.";

            return RedirectToAction("Index");
        }

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
