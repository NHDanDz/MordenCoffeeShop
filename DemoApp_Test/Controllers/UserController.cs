using DemoApp_Test.Models;
using Microsoft.AspNetCore.Mvc;

namespace DemoApp_Test.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            /*            List<User> listUs = new List<User>(); ;
                        for (int i = 0; i < 5; i++)
                        {
                            User us = new User();
                            us.us = "admin";
                            us.mk = "123";
                            listUs.Add(us);
                        }*/
            /*           User us = new User();
                       us.us = "admin";
                       us.mk = "123";
                       ViewBag.abc = us;*/
            //TempData["us"] = "admin";
            //TempData["mk"] = "123";
            List<User> ls = new List<User>();
            for (int i = 0; i < 10; i++)
            {
                User us = new User();
                us.id = 1;
                us.us = "user " + i;
                us.mk = "123";
                ls.Add(us);
            }
            return View(ls);
        }
        public IActionResult Add()
        {
            return View();
        }
        public IActionResult Edit(int id)
        {
            User user = new User();
            user.id = id;
            user.us = "admin";
            user.mk = "123";
            return View(user);
        }
        public IActionResult Details(int id)
        {
            User user = new User();
            user.id = id;
            user.us = "admin";
            user.mk = "123";
            return View(user);
        }


        [HttpPost]
        public IActionResult AddAction()
        {
            string us = Request.Form["us"];
            string mk = Request.Form["mk"];

            if ("admin".Equals(us) && "123".Equals(mk))
            {
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Add");
            }
        }

        [HttpPost]
        public IActionResult Create()
        {
            string us = Request.Form["us"];
            string mk = Request.Form["mk"];
            User user = new User();
            user.us = us;
            user.mk = mk;

            return RedirectToAction("Index");
        }
    }
}
