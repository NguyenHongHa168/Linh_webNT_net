using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using noithat.Common;
using noithat.Models;
using Models.DAO;
using Models.EF;

namespace noithat.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult Index()
        {
            return View();

        }
        [ChildActionOnly]
        public PartialViewResult HeaderCart()
        {
            var cart = Session[Commoncontent.CartSession];
            var list = new List<CartItem>();
            if (cart != null)
            {
                list = (List<CartItem>)cart;
            }

            return PartialView(list);
        }


        public ActionResult ProductFree()
        {
            return View();

        }



    }
}