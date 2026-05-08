using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Models.DAO;
using Models.EF;
using System.Web.Script.Serialization;
using PagedList;
using PagedList.Mvc;
using System.IO;
using noithat.Common;

namespace noithat.Areas.Admin.Controllers
{
    public class ProductCateController : HomeController
    {
        DBNoiThat db = new DBNoiThat();

        public ActionResult Index()
        {
            return View();
        }

        [HasCredential(RoleId = "VIEW_CATE")]
        public ActionResult Show()
        {
            var session = (UserLogin)Session[noithat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            return View(db.Categories.ToList());
        }

        [HttpGet]
        [HasCredential(RoleId = "ADD_CATE")]
        public ActionResult Add()
        {
            var session = (UserLogin)Session[noithat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;
            return View();
        }

        [HttpPost]
        [HasCredential(RoleId = "ADD_CATE")]
        public ActionResult Add(Category n)
        {
            var model = db.Categories.SingleOrDefault(a => a.CategoryId == n.CategoryId);
            if (model != null)
            {
                ModelState.AddModelError("CateError", "CategoryId already in use");
                return View();
            }
            else
            {
                db.Categories.Add(n);
                db.SaveChanges();
                return RedirectToAction("Show");
            }

        }

        [HttpGet]
        [HasCredential(RoleId = "EDIT_CATE")]
        public ActionResult Edit(int CategoryId)
        {
            var session = (UserLogin)Session[noithat.Common.Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            Category a = db.Categories.SingleOrDefault(n => n.CategoryId == CategoryId);
            return View(a);

        }

        [HttpPost]
        [HasCredential(RoleId = "EDIT_CATE")]
        public ActionResult Edit(Category n)
        {
            if (ModelState.IsValid)
            {
                db.Entry(n).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Show");
            }
            else
            {
                return JavaScript("alert('Error');");
            }
        }


        [HttpGet]
        [HttpPost]
        [HasCredential(RoleId = "DELETE_CATE")]
        public ActionResult Delete(int? CategoryId)
        {
            try
            {
                if (!CategoryId.HasValue)
                {
                    TempData["ErrorMessage"] = "Id danh mục không hợp lệ!";
                    return RedirectToAction("Show");
                }

                var model = db.Categories.Find(CategoryId.Value);
                if (model == null)
                {
                    TempData["ErrorMessage"] = "Danh mục không tồn tại!";
                    return RedirectToAction("Show");
                }

                // Kiểm tra có product nào liên kết không
                var hasProducts = db.Products.Any(p => p.CateId == CategoryId.Value);
                if (hasProducts)
                {
                    TempData["ErrorMessage"] = "Không thể xóa danh mục này vì có sản phẩm liên kết!";
                    return RedirectToAction("Show");
                }

                db.Categories.Remove(model);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa: " + ex.Message;
                System.Diagnostics.Debug.WriteLine("Delete Error: " + ex.ToString());
            }

            return RedirectToAction("Show");
        }

    }
}