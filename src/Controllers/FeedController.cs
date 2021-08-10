﻿using Microsoft.AspNetCore.Mvc;
using Phytime.Models;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using Phytime.Services;
using Microsoft.Extensions.Configuration;
using System;

namespace Phytime.Controllers
{
    public class FeedController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IRepository<Feed, User> _feedRepository;

        public FeedController(IConfiguration config, IRepository<Feed, User> repository = null)
        {
            _config = config;
            _feedRepository = repository ?? new FeedRepository(config);
        }

        public ActionResult RssFeed(string url, int page)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            if (page < 1)
            {
                throw new ArgumentException(nameof(page));
            }
            ViewBag.Subscribed = IsSubscribed(url);
            var feedItems = GetSyndicationItems(url);
            if(Request.HasFormContentType)
            {
                var selectedSort = Request.Form["SortValue"].ToString();
                var sorterer = new FeedSorterer();
                sorterer.SortFeed(selectedSort, ref feedItems);
            }
            var rssFeed = FormFeed(url, page, feedItems);
            return View(rssFeed);
        }

        public Feed FormFeed(string url, int page, List<SyndicationItem> items)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            if (page < 1)
            {
                throw new ArgumentException(nameof(page));
            }
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            int pageSize = int.Parse(_config.GetSection("FeedPageInfo:pageSize").Value);
            IEnumerable<SyndicationItem> itemsPerPages = items.Skip((page - 1) * pageSize).Take(pageSize);
            PageInfo pageInfo = new PageInfo { PageNumber = page, PageSize = pageSize, TotalItems = items.Count };
            Feed rssFeed = _feedRepository.GetBy(url);
            rssFeed.PageInfo = pageInfo;
            rssFeed.SyndicationItems = itemsPerPages;
            return rssFeed;
        }

        public List<SyndicationItem> GetSyndicationItems(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            XmlReader reader = XmlReader.Create(url);
            SyndicationFeed feed = SyndicationFeed.Load(reader);
            reader.Close();
            return feed.Items.ToList();
        }

        public bool IsSubscribed(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            var feed = _feedRepository.GetBy(url);
            var user = _feedRepository.GetContainedItemBy(HttpContext.User.Identity.Name);
            return _feedRepository.ContainsItem(feed, user);
        }

        public IActionResult Subscribe(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            var feed = _feedRepository.GetBy(url);
            var user = _feedRepository.GetContainedItemBy(HttpContext.User.Identity.Name);
            _feedRepository.AddItemToContains(feed, user);
            return RedirectToAction("RssFeed", new { url = url, page = 1});
        }

        public IActionResult Unsubscribe(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            var feed = _feedRepository.GetBy(url);
            var user = _feedRepository.GetContainedItemBy(HttpContext.User.Identity.Name);
            _feedRepository.RemoveItemFromContains(feed, user);
            return RedirectToAction("RssFeed", new { url = url, page = 1 });
        }

        public RedirectResult Logout()
        {
            return Redirect("/Account/Logout");
        }

        public IActionResult ShowAngular(int id)
        {
            return Redirect($"/angular/{id}");
        }

        protected override void Dispose(bool disposing)
        {
            _feedRepository.Dispose();
            base.Dispose(disposing);
        }
    }
}
