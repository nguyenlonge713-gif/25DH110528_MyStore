
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using _25DH110528_MyStore.Models;

namespace _25DH110528_MyStore.Areas.Admin.Controllers
{
    public class OrdersController : AdminBaseController
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
            // Tìm đơn hàng
            var order = db.Orders.Find(id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Lấy trạng thái hiện tại
            string currentStatus =
                (order.OrderStatus ?? "").Trim();

            // Kiểm tra đơn hàng đã kết thúc chưa
            bool isCompleted =
                string.Equals(
                    currentStatus,
                    "Đã giao",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    currentStatus,
                    "Đã hủy",
                    StringComparison.OrdinalIgnoreCase);

            // Không cho phép cập nhật đơn hàng đã kết thúc
            if (isCompleted)
            {
                TempData["Error"] =
                    "Đơn hàng đã giao hoặc đã hủy. Không thể thay đổi trạng thái nữa.";

                return RedirectToAction("Index");
            }

            // Các trạng thái hợp lệ
            string[] validStatuses =
            {
                "Chờ xác nhận",
                "Đã xác nhận",
                "Đang giao",
                "Đã giao",
                "Đã hủy"
            };

            // Kiểm tra trạng thái mới
            if (string.IsNullOrWhiteSpace(orderStatus) ||
                !validStatuses.Contains(orderStatus.Trim()))
            {
                TempData["Error"] =
                    "Trạng thái đơn hàng không hợp lệ.";

                return RedirectToAction("Index");
            }

            // Cập nhật trạng thái
            order.OrderStatus = orderStatus.Trim();

            // Lưu vào Database
            db.SaveChanges();

            TempData["Success"] =
                "Cập nhật trạng thái đơn hàng thành công.";

            return RedirectToAction("Index");
        }

        // 4. GIẢI PHÓNG KẾT NỐI DATABASE
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
