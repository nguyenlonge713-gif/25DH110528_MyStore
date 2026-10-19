using _25DH110528_MyStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using PagedList;

namespace _25DH110528_MyStore.Areas.Admin.Controllers
{
    public class ProductsController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Admin/Products
        public ActionResult Index(
            string searchTerm,
            decimal? minPrice,
            decimal? maxPrice,
            string sortOrder,
            int? page)
        {
            var products = db.Products
                             .Include(p => p.Category)
                             .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p =>
                    p.ProductName.Contains(searchTerm) ||
                    p.ProductDescription.Contains(searchTerm) ||
                    p.Category.CategoryName.Contains(searchTerm));
            }

            // Lọc giá từ
            if (minPrice.HasValue)
            {
                products = products.Where(p =>
                    p.ProductPrice >= minPrice.Value);
            }

            // Lọc giá đến
            if (maxPrice.HasValue)
            {
                products = products.Where(p =>
                    p.ProductPrice <= maxPrice.Value);
            }

            // Sắp xếp
            switch (sortOrder)
            {
                case "name_desc":
                    products = products.OrderByDescending(p => p.ProductName);
                    break;

                case "price_asc":
                    products = products.OrderBy(p => p.ProductPrice);
                    break;

                case "price_desc":
                    products = products.OrderByDescending(p => p.ProductPrice);
                    break;

                default:
                    products = products.OrderBy(p => p.ProductName);
                    break;
            }

            // Số sản phẩm trên mỗi trang
            int pageSize = 5;

            // Trang hiện tại
            int pageNumber = page ?? 1;

            // Tạo ViewModel
            var model = new _25DH110528_MyStore.Models.ViewModel.ProductSearchVM
            {
                Products = products.ToPagedList(pageNumber, pageSize),
                SearchTerm = searchTerm,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortOrder = sortOrder
            };

            return View(model);
        }


        // GET: Admin/Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }


        // GET: Admin/Products/Create
        public ActionResult Create()
        {
            LoadCategories();

            return View();
        }


        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product product, HttpPostedFileBase ImageFile)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh khi thêm mới
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    string fileName = System.IO.Path.GetFileName(ImageFile.FileName);
                    string path = System.IO.Path.Combine(Server.MapPath("~/Content/images/"), fileName);
                    ImageFile.SaveAs(path);
                    product.ProductImage = "~/Content/images/" + fileName;
                }
                else
                {
                    product.ProductImage = "~/Content/images/default.png";
                }

                SaveProduct(product, "Create");

                return RedirectToAction("Index");
            }

            LoadCategories(product.CategoryID);

            return View(product);
        }


        // GET: Admin/Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            LoadCategories(product.CategoryID);

            return View(product);
        }


        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product product, HttpPostedFileBase ImageFile)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh khi chỉnh sửa
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    string fileName = System.IO.Path.GetFileName(ImageFile.FileName);
                    string path = System.IO.Path.Combine(Server.MapPath("~/Content/images/"), fileName);
                    ImageFile.SaveAs(path);
                    product.ProductImage = "~/Content/images/" + fileName;
                }
                else
                {
                    // Nếu không chọn ảnh mới, giữ nguyên đường dẫn ảnh cũ trong CSDL
                    var existingProduct = db.Products.AsNoTracking().FirstOrDefault(p => p.ProductID == product.ProductID);
                    if (existingProduct != null)
                    {
                        product.ProductImage = existingProduct.ProductImage;
                    }
                }

                SaveProduct(product, "Edit");

                return RedirectToAction("Index");
            }

            LoadCategories(product.CategoryID);

            return View(product);
        }


        // GET: Admin/Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }


        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            SaveProduct(product, "Delete");

            return RedirectToAction("Index");
        }


        // HÀM CON DÙNG CHUNG CHO CREATE, EDIT VÀ DELETE
        private void SaveProduct(Product product, string action)
        {
            if (action == "Create")
            {
                db.Products.Add(product);
            }
            else if (action == "Edit")
            {
                db.Entry(product).State = EntityState.Modified;
            }
            else if (action == "Delete")
            {
                db.Products.Remove(product);
            }

            db.SaveChanges();
        }


        // HÀM DÙNG CHUNG ĐỂ LOAD DANH MỤC
        private void LoadCategories(object selectedCategory = null)
        {
            ViewBag.CategoryID = new SelectList(
                db.Categories,
                "CategoryID",
                "CategoryName",
                selectedCategory
            );
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