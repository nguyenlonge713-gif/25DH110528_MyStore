using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using _25DH110528_MyStore.Models;

namespace _25DH110528_MyStore.Areas.Admin.Controllers
{
    public class OrdersController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // 1. HIỂN THỊ DANH SÁCH ĐƠN HÀNG
        // GET: Admin/Orders
        public ActionResult Index()
        {
            var orders = db.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // 2. HIỂN THỊ CHI TIẾT ĐƠN HÀNG
        // GET: Admin/Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            var order = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // 3. CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG
        // POST: Admin/Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string orderStatus)
        {
            // Các trạng thái đơn hàng được phép sử dụng
            string[] validStatuses =
            {
                "Chờ xác nhận",
                "Đã xác nhận",
                "Đang giao",
                "Đã giao",
                "Đã hủy"
            };

            // Kiểm tra trạng thái được gửi lên
            if (string.IsNullOrWhiteSpace(orderStatus) ||
                !validStatuses.Contains(orderStatus))
            {
                TempData["Error"] =
                    "Trạng thái đơn hàng không hợp lệ.";

                return RedirectToAction("Index");
            }

            // Tìm đơn hàng theo mã
            var order = db.Orders.Find(id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Không cho phép thay đổi đơn hàng đã kết thúc
            if (order.OrderStatus == "Đã giao" ||
                order.OrderStatus == "Đã hủy")
            {
                TempData["Error"] =
                    "Đơn hàng đã giao hoặc đã hủy, không thể cập nhật.";

                return RedirectToAction("Index");
            }

            // Cập nhật trạng thái mới
            order.OrderStatus = orderStatus;

            // Lưu thay đổi vào cơ sở dữ liệu
            db.SaveChanges();

            TempData["Success"] =
                "Cập nhật trạng thái đơn hàng thành công.";

            return RedirectToAction("Index");
        }

        // 4. GIẢI PHÓNG KẾT NỐI CƠ SỞ DỮ LIỆU
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
