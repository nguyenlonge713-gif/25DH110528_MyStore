using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using _25DH110528_MyStore.Models;

namespace _25DH110528_MyStore.Areas.Admin.Controllers
{
    public class CategoriesController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Admin/Categories
        public ActionResult Index()
        {
            var categories = db.Categories.ToList();
            return View(categories);
        }

        // GET: Admin/Categories/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            Category category = db.Categories.Find(id);

            if (category == null)
            {
                return HttpNotFound();
            }

            return View(category);
        }

        // GET: Admin/Categories/Create
        public ActionResult Create()
        {
            return View(new Category());
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            [Bind(Include = "CategoryID,CategoryName")]
            Category category)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                ViewBag.CreateError = string.Join(" | ", errors);

                return View(category);
            }

            try
            {
                // Chuẩn hóa tên danh mục
                if (category.CategoryName != null)
                {
                    category.CategoryName =
                        category.CategoryName.Trim();
                }

                // Kiểm tra tên danh mục rỗng
                if (string.IsNullOrWhiteSpace(category.CategoryName))
                {
                    ViewBag.CreateError =
                        "Vui lòng nhập tên danh mục.";

                    return View(category);
                }

                // Thêm danh mục vào database
                db.Categories.Add(category);
                db.SaveChanges();

                // Thêm thành công, quay về danh sách
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Hiển thị lỗi để tiện kiểm tra
                Exception error = ex;

                while (error.InnerException != null)
                {
                    error = error.InnerException;
                }

                ViewBag.CreateError = error.Message;

                return View(category);
            }
        }

        // GET: Admin/Categories/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            Category category = db.Categories.Find(id);

            if (category == null)
            {
                return HttpNotFound();
            }

            return View(category);
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            [Bind(Include = "CategoryID,CategoryName")]
            Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            Category existingCategory =
                db.Categories.Find(category.CategoryID);

            if (existingCategory == null)
            {
                return HttpNotFound(
                    "Không tìm thấy danh mục cần cập nhật.");
            }

            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "Vui lòng nhập tên danh mục.");

                return View(category);
            }

            try
            {
                existingCategory.CategoryName =
                    category.CategoryName.Trim();

                db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật danh mục. "
                    + "Vui lòng kiểm tra lại dữ liệu.");

                return View(category);
            }
        }

        // GET: Admin/Categories/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            Category category = db.Categories.Find(id);

            if (category == null)
            {
                return HttpNotFound();
            }

            return View(category);
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Category category = db.Categories.Find(id);

            if (category == null)
            {
                return HttpNotFound();
            }

            int productCount = db.Products.Count(
                p => p.CategoryID == id);

            if (productCount > 0)
            {
                TempData["DeleteError"] =
                    "Không thể xóa danh mục này vì đang có "
                    + productCount
                    + " sản phẩm thuộc danh mục.";

                return RedirectToAction("Delete", new { id = id });
            }

            try
            {
                db.Categories.Remove(category);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["DeleteError"] =
                    "Không thể xóa danh mục. "
                    + "Vui lòng kiểm tra dữ liệu liên quan.";

                return RedirectToAction("Delete", new { id = id });
            }
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