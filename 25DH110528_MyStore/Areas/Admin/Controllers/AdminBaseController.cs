
using _25DH110528_MyStore.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace _25DH110528_MyStore.Areas.Admin.Controllers
{
    public class AdminBaseController : Controller
    {
        protected override void OnActionExecuting(
            ActionExecutingContext filterContext)
        {
            // 1. Chưa đăng nhập:
            // Không chuyển đến Login, chỉ chuyển về trang chủ.
            if (Session["Username"] == null)
            {
                filterContext.Result = RedirectToAction(
                    "Index",
                    "Home",
                    new { area = "" });

                return;
            }

            string username = Session["Username"].ToString();

            // 2. Kiểm tra tài khoản trong Database
            using (var db = new MyStoreEntities())
            {
                var user = db.Users.FirstOrDefault(
                    u => u.Username == username);

                // Tài khoản không còn tồn tại
                if (user == null)
                {
                    Session.Clear();
                    Session.Abandon();

                    filterContext.Result = RedirectToAction(
                        "Index",
                        "Home",
                        new { area = "" });

                    return;
                }

                // 3. Lấy vai trò mới nhất
                string currentRole = (user.UserRole ?? "").Trim();

                Session["UserRole"] = currentRole;

                // 4. Không phải Admin
                if (!string.Equals(
                    currentRole,
                    "A",
                    StringComparison.OrdinalIgnoreCase))
                {
                    filterContext.Result = RedirectToAction(
                        "Index",
                        "Home",
                        new { area = "" });

                    return;
                }
            }

            // 5. Nếu là Admin thì cho phép tiếp tục
            base.OnActionExecuting(filterContext);
        }
    }
}
