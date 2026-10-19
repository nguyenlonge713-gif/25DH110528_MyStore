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
        public ActionResult Index(int? categoryId, string searchTerm)
        {
            var products = db.Products.AsQueryable();

            // 1. Lọc theo danh mục nếu người dùng chọn danh mục bên sidebar
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryId.Value);
                ViewBag.SelectedCategory = categoryId.Value;
            }

            // 2. Lọc theo từ khóa tìm kiếm (nếu có nhập)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p => p.ProductName.Contains(searchTerm) || p.ProductDescription.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            // 3. Lấy danh sách danh mục để hiển thị menu bên trái trang chủ
            ViewBag.Categories = db.Categories.ToList();

            // Trả về danh sách sản phẩm sau khi lọc/tìm kiếm
            return View(products.OrderByDescending(p => p.ProductID).ToList());
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

            return View(product);
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