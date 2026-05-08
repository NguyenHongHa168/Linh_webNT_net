using Models.DAO;
using Models.EF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using noithat.Common;
using noithat.Models;

namespace noithat.Areas.Admin.Controllers
{
    public class ProductController : HomeController
    {
        DBNoiThat db = new DBNoiThat();

        [HasCredential(RoleId = "VIEW_PRODUCT")]
        public ActionResult Show()
        {
            var session = (UserLogin)Session[noithat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            var productViewModels = (from a in db.Products
                         join b in db.Providers on a.ProviderId equals b.ProviderId
                         join c in db.Categories on a.CateId equals c.CategoryId
                         select new ProductViewModel
                         {
                             ProductId = a.ProductId,
                             Name = a.Name,
                             Description = a.Description,
                             Discount = a.Discount,
                             ProviderName = b.Name,
                             CateName = c.Name,
                             Price = a.Price,
                             Quantity = a.Quantity,
                             StartDate = a.StartDate,
                             EndDate = a.EndDate,
                             Photo = a.Photo,
                         }).ToList();

            return View(productViewModels);
        }

        [HttpGet]
        [HasCredential(RoleId = "ADD_PRODUCT")]
        public ActionResult Add()
        {
            var session = (UserLogin)Session[noithat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            ViewBag.ListCate = new SelectList(db.Categories.ToList(), "CategoryId", "Name");
            ViewBag.ListProvider = new SelectList(db.Providers.ToList(), "ProviderId", "Name");
            return View();
        }

        [HttpPost]
        [HasCredential(RoleId = "ADD_PRODUCT")]
        public ActionResult Add(ProductViewModel n, HttpPostedFileBase Photo)
        {
            var session = (UserLogin)Session[noithat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            ViewBag.ListCate = new SelectList(db.Categories.ToList(), "CategoryId", "Name");
            ViewBag.ListProvider = new SelectList(db.Providers.ToList(), "ProviderId", "Name");
            
            // Debug: Kiểm tra file upload
            System.Diagnostics.Debug.WriteLine($"Photo != null: {Photo != null}");
            System.Diagnostics.Debug.WriteLine($"Photo ContentLength: {Photo?.ContentLength ?? -1}");
            
            // Kiểm tra file upload
            if (Photo == null || Photo.ContentLength == 0)
            {
                ModelState.AddModelError("Photo", "Vui lòng chọn hình ảnh!");
            }
            
            // Kiểm tra ngày
            if (n.StartDate >= n.EndDate)
            {
                ModelState.AddModelError("DateRange", "Ngày kết thúc phải muộn hơn ngày bắt đầu!");
            }
            
            // Debug: Hiển thị tất cả lỗi
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                }
            }
            if (ModelState.IsValid)
            {
                var models = db.Products.SingleOrDefault(a => a.ProductId == n.ProductId);
                if (models != null)
                {
                    ModelState.AddModelError("ProductError", "Mã sản phẩm đã tồn tại!");
                    return View(n);
                }

                try
                {
                    string folderPath = Server.MapPath("~/image");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(Photo.FileName);
                    var path = Path.Combine(folderPath, fileName);
                    Photo.SaveAs(path);

                    var model = new Product();
                    model.ProductId = n.ProductId;
                    model.Name = n.Name;
                    model.Photo = fileName;
                    model.Price = n.Price;
                    model.Quantity = n.Quantity;
                    model.StartDate = n.StartDate;
                    model.EndDate = n.EndDate;
                    model.CateId = n.CateId;
                    model.Description = n.Description;
                    model.Discount = n.Discount ?? 0;
                    model.ProviderId = n.ProviderId;

                    db.Products.Add(model);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction("Show");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi lưu: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("Error: " + ex.ToString());
                }
            }

            return View(n);
        }

        [HttpGet]
        [HasCredential(RoleId = "EDIT_PRODUCT")]
        public ActionResult Edit(int ProductId)
        {
            var session = (UserLogin)Session[noithat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            var model = (from a in db.Products
                         join b in db.Providers on a.ProviderId equals b.ProviderId
                         join c in db.Categories on a.CateId equals c.CategoryId
                         where a.ProductId == ProductId
                         select new ProductViewModel
                         {
                             ProductId = a.ProductId,
                             Name = a.Name,
                             Description = a.Description,
                             Discount = a.Discount,
                             ProviderName = b.Name,
                             CateName = c.Name,
                             Price = a.Price,
                             Quantity = a.Quantity,
                             StartDate = a.StartDate,
                             EndDate = a.EndDate,
                             Photo = a.Photo,
                         }).ToList();

            ViewBag.ListCate = new SelectList(db.Categories.ToList(), "CategoryId", "Name");
            ViewBag.ListProvider = new SelectList(db.Providers.ToList(), "ProviderId", "Name");
            var models = model.Where(n => n.ProductId == ProductId).First();
            return View(models);
        }

        [HttpPost]
        [HasCredential(RoleId = "EDIT_PRODUCT")]
        public ActionResult Edit(ProductViewModel n, HttpPostedFileBase UploadImage)
        {
            ViewBag.ListCate = new SelectList(db.Categories.ToList(), "CategoryId", "Name");
            ViewBag.ListProvider = new SelectList(db.Providers.ToList(), "ProviderId", "Name");
            if (ModelState.IsValid)
            {
                try
                {
                    var model = db.Products.FirstOrDefault(m => m.ProductId == n.ProductId);
                    if (model == null)
                    {
                        ModelState.AddModelError("", "Sản phẩm không tồn tại!");
                        return View(n);
                    }

                    if (UploadImage != null && UploadImage.ContentLength > 0)
                    {
                        // Delete exiting file
                        //System.IO.File.Delete(Path.Combine(Server.MapPath("~/image"), model.Photo));
                        // Save new file
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(UploadImage.FileName);
                        string path = Path.Combine(Server.MapPath("~/image"), fileName);
                        UploadImage.SaveAs(path);
                        model.Photo = fileName;
                    }
                    else if (!string.IsNullOrEmpty(n.Photo))
                    {
                        model.Photo = n.Photo;
                    }

                    model.ProductId = n.ProductId;
                    model.Name = n.Name;
                    model.Price = n.Price;
                    model.Quantity = n.Quantity;
                    model.StartDate = n.StartDate;
                    model.EndDate = n.EndDate;
                    model.CateId = n.CateId;
                    model.Description = n.Description;
                    model.Discount = n.Discount;
                    model.ProviderId = n.ProviderId;
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction("Show");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("Edit Error: " + ex.ToString());
                }
            }
            else
            {
                ModelState.AddModelError("", "Vui lòng kiểm tra lại dữ liệu nhập vào");
            }
            return View(n);
        }

        //[HttpGet]
        //[HasCredential(RoleId = "DELETE_PRODUCT")]
        //public ActionResult Delete()
        //{
        //    var session = (UserLogin)Session[noithat.Common.Commoncontent.user_sesion_admin];
        //    ViewBag.username = session.Username;
        //    return View();
        //}
        [HttpGet]
        [HasCredential(RoleId = "DELETE_PRODUCT")]
        [ValidateInput(false)]
        public ActionResult Delete(int? ProductId)
        {
            if (!ProductId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var model = db.Products.Find(ProductId.Value);
            if (model == null)
                return HttpNotFound();

            db.Products.Remove(model);
            db.SaveChanges();
            TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";

            return RedirectToAction("Show");
        }

        public ActionResult Menu()
        {
            var session = (UserLogin)Session[noithat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;
            //Danh sách loại sản phẩm
            var model = new CategoryDao().ListCategory();
            return PartialView(model);
        }
    }
}
