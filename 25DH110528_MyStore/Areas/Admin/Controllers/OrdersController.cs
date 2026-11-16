
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

        public ActionResult Index()
        {
            var orders = db.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

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
