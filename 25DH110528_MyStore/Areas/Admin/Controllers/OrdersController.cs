
using System;
using System.Data;
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

        // DANH SÁCH ĐƠN HÀNG + TÌM KIẾM VÀ LỌC
        public ActionResult Index(
            string customerName,
            string paymentStatus,
            DateTime? orderDate)
        {
            var orders = db.Orders
                .Include(o => o.Customer)
                .AsQueryable();

            // 1. Tìm theo tên khách hàng
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                customerName = customerName.Trim();

                orders = orders.Where(o =>
                    o.Customer != null &&
                    o.Customer.CustomerName.Contains(customerName));
            }

            // 2. Lọc theo trạng thái thanh toán
            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                if (paymentStatus == "Chưa cập nhật")
                {
                    orders = orders.Where(o =>
                        o.PaymentStatus == null ||
                        o.PaymentStatus == "");
                }
                else
                {
                    orders = orders.Where(o =>
                        o.PaymentStatus == paymentStatus);
                }
            }

            // 3. Lọc theo ngày đặt hàng
            if (orderDate.HasValue)
            {
                DateTime startDate = orderDate.Value.Date;
                DateTime endDate = startDate.AddDays(1);

                orders = orders.Where(o =>
                    o.OrderDate >= startDate &&
                    o.OrderDate < endDate);
            }

            // Sắp xếp đơn hàng mới nhất lên đầu
            var result = orders
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            // Giữ lại giá trị bộ lọc trên giao diện
            ViewBag.CustomerName = customerName;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.OrderDate = orderDate.HasValue
                ? orderDate.Value.ToString("yyyy-MM-dd")
                : "";

            return View(result);
        }

        // CHI TIẾT ĐƠN HÀNG
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var order = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        // CẬP NHẬT TRẠNG THÁI THANH TOÁN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePaymentStatus(
            int id,
            string paymentStatus,
            string customerName,
            string filterPaymentStatus,
            DateTime? orderDate)
        {
            string[] validStatuses =
            {
        "Chưa thanh toán",
        "Đã thanh toán",
        "Chưa cập nhật"
    };

            if (string.IsNullOrWhiteSpace(paymentStatus) ||
                !validStatuses.Contains(paymentStatus.Trim()))
            {
                TempData["Error"] = "Trạng thái thanh toán không hợp lệ.";
            }
            else
            {
                try
                {
                    var order = db.Orders.FirstOrDefault(o => o.OrderID == id);

                    if (order == null)
                    {
                        TempData["Error"] = "Không tìm thấy đơn hàng.";
                    }
                    else
                    {
                        order.PaymentStatus = paymentStatus.Trim();

                        db.SaveChanges();

                        TempData["Success"] =
                            "Cập nhật trạng thái thanh toán thành công.";
                    }
                }
                catch (Exception)
                {
                    TempData["Error"] =
                        "Có lỗi xảy ra khi cập nhật trạng thái thanh toán.";
                }
            }

            return RedirectToAction("Index", new
            {
                customerName = customerName,
                paymentStatus = filterPaymentStatus,
                orderDate = orderDate.HasValue
                    ? orderDate.Value.ToString("yyyy-MM-dd")
                    : null
            });
        }

        // CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string orderStatus)
        {
            string[] validStatuses =
            {
                "Chờ xác nhận",
                "Đã xác nhận",
                "Đang giao",
                "Đã giao",
                "Đã hủy"
            };

            if (string.IsNullOrWhiteSpace(orderStatus) ||
                !validStatuses.Contains(orderStatus.Trim()))
            {
                TempData["Error"] = "Trạng thái đơn hàng không hợp lệ.";
                return RedirectToAction("Index");
            }

            orderStatus = orderStatus.Trim();

            using (var transaction =
                db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var order = db.Orders
                        .FirstOrDefault(o => o.OrderID == id);

                    if (order == null)
                    {
                        transaction.Rollback();
                        return HttpNotFound();
                    }

                    string currentStatus = (order.OrderStatus ?? "").Trim();

                    // Không cho thay đổi đơn hàng đã giao hoặc đã hủy
                    if (currentStatus == "Đã giao" ||
                        currentStatus == "Đã hủy")
                    {
                        transaction.Rollback();

                        TempData["Error"] =
                            "Đơn hàng đã giao hoặc đã hủy, không thể thay đổi trạng thái.";

                        return RedirectToAction("Index");
                    }

                    // Chỉ kiểm tra và trừ kho khi chuyển sang Đã giao
                    if (orderStatus == "Đã giao")
                    {
                        var orderDetails = db.OrderDetails
                            .Where(d => d.OrderID == id)
                            .ToList();

                        if (!orderDetails.Any())
                        {
                            transaction.Rollback();

                            TempData["Error"] =
                                "Đơn hàng không có sản phẩm.";

                            return RedirectToAction("Index");
                        }

                        // Gộp số lượng theo ProductID
                        var productQuantities = orderDetails
                            .GroupBy(d => d.ProductID)
                            .Select(g => new
                            {
                                ProductID = g.Key,
                                Quantity = g.Sum(x => x.Quantity)
                            })
                            .ToList();

                        // Kiểm tra toàn bộ tồn kho trước khi trừ
                        foreach (var item in productQuantities)
                        {
                            var product = db.Products
                                .FirstOrDefault(p =>
                                    p.ProductID == item.ProductID);

                            if (product == null)
                            {
                                transaction.Rollback();

                                TempData["Error"] =
                                    "Không tìm thấy sản phẩm trong đơn hàng.";

                                return RedirectToAction("Index");
                            }

                            if (item.Quantity <= 0 ||
                                product.StockQuantity < item.Quantity)
                            {
                                transaction.Rollback();

                                TempData["Error"] =
                                    "Sản phẩm \"" + product.ProductName +
                                    "\" không đủ tồn kho. Tồn kho hiện tại: " +
                                    product.StockQuantity +
                                    ", số lượng cần giao: " + item.Quantity;

                                return RedirectToAction("Index");
                            }
                        }

                        // Tất cả sản phẩm đủ hàng mới bắt đầu trừ kho
                        foreach (var item in productQuantities)
                        {
                            var product = db.Products
                                .First(p => p.ProductID == item.ProductID);

                            product.StockQuantity -= item.Quantity;
                        }
                    }

                    // Cập nhật trạng thái đơn hàng
                    order.OrderStatus = orderStatus;

                    db.SaveChanges();

                    transaction.Commit();

                    TempData["Success"] =
                        orderStatus == "Đã giao"
                            ? "Đã giao hàng và cập nhật tồn kho thành công."
                            : "Cập nhật trạng thái đơn hàng thành công.";

                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    TempData["Error"] =
                        "Có lỗi xảy ra khi cập nhật đơn hàng. Vui lòng thử lại.";

                    return RedirectToAction("Index");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
