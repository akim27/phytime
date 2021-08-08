﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Phytime.Models;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;

namespace Phytime.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : Controller
    {
        private readonly IRepository _repository;

        public ItemsController(IConfiguration config, IRepository repository = null)
        {
            _repository = repository ?? new PhytimeRepository(config);
        }

        [HttpGet("{id:int}")]
        public IEnumerable<Item> Get(int id)
        {
            return GetItems(id);
        }

        private List<Item> GetItems(int id)
        {
            var feed = _repository.GetFeed(id);
            var list = GetSyndicationItems(feed.Url);
            return CreateItemsList(list);
        }

        public List<SyndicationItem> GetSyndicationItems(string url)
        {
            XmlReader reader = XmlReader.Create(url);
            SyndicationFeed feed = SyndicationFeed.Load(reader);
            reader.Close();
            return feed.Items.ToList();
        }

        public List<Item> CreateItemsList(List<SyndicationItem> list)
        {
            var itemsList = new List<Item>();
            foreach(var item in list)
            {
                itemsList.Add(new Item { Title = item.Title.Text, 
                    Summary = item.Summary.Text, Publishdate = item.PublishDate.ToString("D") });
            }
            return itemsList;
        }

        protected override void Dispose(bool disposing)
        {
            (_repository as PhytimeRepository).Dispose();
            base.Dispose(disposing);
        }
    }
}
