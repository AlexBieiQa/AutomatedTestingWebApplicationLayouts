﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Hangfire;
using HtmlAgilityPack;
using Microsoft.AspNet.Identity;
using WebApplication1.Models;
using WebApplication1.Entities;
using WebApplication1.Helpers;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {

        public ApplicationDbContext db { get; set; } = new ApplicationDbContext();

        public ActionResult Index()
        {
            if(User.Identity.IsAuthenticated)
                return RedirectToAction("Sites");
            return View();
        }


        [Authorize]
        public ActionResult Sites()
        {
            var userId = User.Identity.GetUserId();
            var sites = db.Sites.Where(z => z.User.Id == userId).ToList();

            var model = db.Sites.Select(s => new NewSiteModel
            {
                SiteId = s.Id,
                Name = s.Name,
                CountOfPages = s.Links.Count,
                Url = s.Url

            }).ToList();

            return View(model);
        }

        [Authorize]
        public ActionResult SitePages(int SiteId)
        {
            var links = db.Links.Where(z => z.Site.Id == SiteId).ToList();
            
            return View(links);
        }


        [HttpGet]
        [Authorize]
        public ActionResult AddNewSite()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult AddNewSite(NewSiteModel model)
        {

            var userId = User.Identity.GetUserId();
            var user = db.Users.FirstOrDefault(z => z.Id == userId);

            var newSite = new Site()
            {
                Name = model.Name,
                Url = model.Url,
                User = user
            };


            var links = model.Links.Select(z => new Link() {ValueUrl = z, Site = newSite}).ToList();


            //newSite.Links = new List<Link>();
            //newSite.Links.AddRange(links);

            db.Sites.Add(newSite);

            db.Links.AddRange(links);

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [Authorize]
        public ActionResult StartTest(int SiteId)
        {
            var Site = db.Sites.FirstOrDefault(z => z.Id == SiteId);

            if (Site == null)
            {
                return Json(new {success = false, message = "Site not found "}, JsonRequestBehavior.AllowGet);
            }


            var Test = new Test()
            {
                Site = Site, Date = DateTime.Now
            };

            db.Tests.Add(Test);
            db.SaveChanges();

            var testHelper = new TestsHelper();

            BackgroundJob.Enqueue(() => testHelper.StartTest(Test.Id, Site.Id));

            return Json(new {success = true}, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public ActionResult GetLinksFromSite(string Url)
        {
            if (!Url.Contains("http"))
            {
                Url = "https://" + Url;
            }
            var siteUrl = Url;
            var doc = new HtmlWeb().Load(siteUrl);


            var linkedPages = (doc.DocumentNode.Descendants("a")
                .Select(a => a.GetAttributeValue("href", null))
                .Where(u => !String.IsNullOrEmpty(u) && u.Contains(siteUrl))).ToList();

            var stack = new Stack<string>(linkedPages);
            while (stack.Any())
            {
                var tempDoc = new HtmlWeb().Load(stack.First());
                var additionalLinks = tempDoc.DocumentNode.Descendants("a")
                    .Select(a => a.GetAttributeValue("href", null))
                    .Where(u => !String.IsNullOrEmpty(u) && u.Contains(siteUrl) && linkedPages.All(k => k != u));
                linkedPages.AddRange(additionalLinks);


                foreach (var link in additionalLinks)
                {
                    stack.Push(link);
                }

                stack.Pop();

            }
            linkedPages.RemoveAll(u => u.EndsWith(".pdf"));
            linkedPages.RemoveAll(u => u.EndsWith(".doc"));
            linkedPages.RemoveAll(u => u.EndsWith(".docx"));
            linkedPages.RemoveAll(u => u.EndsWith(".jpeg"));
            linkedPages.RemoveAll(u => u.EndsWith(".jpg"));
            linkedPages.RemoveAll(u => u.EndsWith(".png"));
            linkedPages.RemoveAll(u => u.EndsWith(".bmp"));
            linkedPages.RemoveAll(u => u.EndsWith(".gif"));
            linkedPages.RemoveAll(u => u.EndsWith(".rss"));
            





            return Json(linkedPages, JsonRequestBehavior.AllowGet);
        }

        //[Authorize]
        //public ActionResult TakeUrlScreenshots()
        //{
            
        //}


    }

}