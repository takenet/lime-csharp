using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Lime.Web.Controllers
{
    public class HomeController : Controller
    {
        const string DEFAULT_LANGUAGE = "en-US";

        public ActionResult Index(string language)
        {
            return LocalizedView("Index", language);            
        }

        public ActionResult Libraries(string language)
        {
            return LocalizedView("Libraries", language);
        }

        public ActionResult About(string language)
        {
            ViewBag.Message = "Your application description page.";

            return LocalizedView("About", language);
        }

        public ActionResult ContentTypes(string language)
        {
            return LocalizedView("ContentTypes", language);
        }

        public ActionResult Resources(string language)
        {
            return LocalizedView("Resources", language);
        }

        private ViewResult LocalizedView(string viewName, string language = null)
        {            
            if (language == null )
            {
                if (Request.UserLanguages.Any())
                {
                    language = Request.UserLanguages.First();
                }
                else
                {
                    return View(viewName);
                }            
            }

            string localizedViewName = GetLocalizedViewName(viewName, language);

            var searchViewResult = ViewEngines.Engines.FindView(ControllerContext, localizedViewName, null);

            if (searchViewResult == null ||
                searchViewResult.View == null)
            {
                localizedViewName = GetLocalizedViewName(viewName, DEFAULT_LANGUAGE);
            }

            return View(localizedViewName);
        }

        private string GetLocalizedViewName(string viewName, string language)
        {
            return string.Format("{0}.{1}", viewName, language);
        }

    }
}