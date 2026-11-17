using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using _25DH110528_MyStore.Models; // Namespace chứa DbContext và Models của bạn

namespace _25DH110528_MyStore.Controllers
{
    public class HomeController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities(); // Khởi tạo kết nối cơ sở dữ liệu


        // GET: Home/Index
        public ActionResult Index(int? categoryId, string searchTerm, int page = 1)
        {
            // So san pham hien thi tren moi trang
            int pageSize = 8;

            if (page < 1)
            {
                page = 1;
            }

            // Truy van san pham
            var products = db.Products.AsQueryable();

            // 1. Loc theo danh muc
            if (categoryId.HasValue)
            {
                products = products.Where(
                    p => p.CategoryID == categoryId.Value);

                ViewBag.SelectedCategory = categoryId.Value;
            }

            // 2. Tim kiem san pham
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                products = products.Where(p =>
                    (p.ProductName != null &&
                     p.ProductName.Contains(searchTerm)) ||

                    (p.ProductDescription != null &&
                     p.ProductDescription.Contains(searchTerm)));
            }

            // 3. Lay danh sach danh muc
            ViewBag.Categories = db.Categories.ToList();

            // 4. Dem tong so san pham sau khi loc
            int totalProducts = products.Count();

            // 5. Tinh tong so trang
            int totalPages = (int)Math.Ceiling(
                (double)totalProducts / pageSize);

            // Dam bao trang hien tai hop le
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // 6. Lay san pham cua trang hien tai
            var productList = products
                .OrderByDescending(p => p.ProductID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 7. Truyen thong tin sang View
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.PageSize = pageSize;

            return View(productList);
        }


        // GET: Home/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách sản phẩm liên quan cùng danh mục (tối đa 4 sản phẩm)
            var relatedProducts = db.Products
                .Where(p => p.CategoryID == product.CategoryID && p.ProductID != product.ProductID)
                .Take(4)
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;

            // Sử dụng lại giao diện ProductDetail.cshtml
            return View("ProductDetail", product);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }
        // XEM CHI TIẾT SẢN PHẨM
        public ActionResult ProductDetail(int id)
        {
            var product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách sản phẩm liên quan (cùng danh mục)
            var relatedProducts = db.Products.Where(p => p.CategoryID == product.CategoryID && p.ProductID != id)
                                             .Take(4)
                                             .ToList();
            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }
    }
}