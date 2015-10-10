using System;
using System.Linq;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using JetBrains.Annotations;
using Contrib.Podcasts.Models;
using Orchard.ContentManagement;
using Orchard.Core.Feeds;
using Orchard.Core.Feeds.Models;
using Orchard.Core.Feeds.StandardBuilders;
using System.Collections.Generic;
using Orchard.Services;
using System.Web.Mvc;
using Orchard.Core.Common.Models;
using Orchard.Utility.Extensions;
using Contrib.Podcasts.Services;
using Orchard.Settings;
using Orchard.Caching;
using Orchard;

namespace Contrib.Podcasts.Feeds {
  [UsedImplicitly]
  public class PodcastOnFeedQuery : IFeedQueryProvider, IFeedQuery {
    private readonly IContentManager _contentManager;
    private readonly IPodcastService _podcastService;
    private readonly IPodcastEpisodeService _podcastEpisodeService;
    private readonly IEnumerable<IHtmlFilter> _htmlFilters;

    public PodcastOnFeedQuery(IContentManager contentManager, IPodcastService podcastService, IPodcastEpisodeService podcastEpisodeService) {
      _contentManager = contentManager;
      _podcastService = podcastService;
      _podcastEpisodeService = podcastEpisodeService;

    }

    public FeedQueryMatch Match(FeedContext context) {
      var containerIdValue = context.ValueProvider.GetValue("containerid");
      if (containerIdValue == null)
        return null;

      var containerId = (int)containerIdValue.ConvertTo(typeof(int));
      var container = _contentManager.Get(containerId);

      if (container == null) {
        return null;
      }

      return new FeedQueryMatch { FeedQuery = this, Priority = -5 };
    }
    public void Execute(FeedContext context) {
      var containerIdValue = context.ValueProvider.GetValue("containerid");
      if (containerIdValue == null)
        return;

      var limitValue = context.ValueProvider.GetValue("limit");
      var limit = 20;
      if (limitValue != null)
        limit = (int)limitValue.ConvertTo(typeof(int));

      var containerId = (int)containerIdValue.ConvertTo(typeof(int));
      var container = _contentManager.Get(containerId);

      if (container == null) {
        return;
      }

      PodcastPart podcastPart = _podcastService.Get(containerId).As<PodcastPart>();
      var inspector = new ItemInspector(container, _contentManager.GetItemMetadata(container), _htmlFilters);
      if (context.Format == "rss") {
        // add namespace
        XNamespace dcNS = "http://purl.org/dc/elements/1.1/";
        context.Response.Element.Parent.Add(new XAttribute(XNamespace.Xmlns + "dc", dcNS.NamespaceName));


        var link = new XElement("link");

        context.Response.Element.SetElementValue("title", podcastPart.Title);
        context.Response.Element.Add(link);
        context.Response.Element.SetElementValue("description", podcastPart.Description);
        context.Response.Contextualize(requestContext => {
          var urlHelper = new UrlHelper(requestContext);
          var uriBuilder = new UriBuilder(urlHelper.RequestContext.HttpContext.Request.ToRootUrlString()) { Path = urlHelper.RouteUrl(inspector.Link) };
          link.Add(uriBuilder.Uri.OriginalString);
        });
        context.Response.Element.Add(new XElement("lastBuildDate", DateTime.UtcNow.ToString("r")));
        context.Response.Element.Add(new XElement("copyright", "Copyright " + DateTime.UtcNow.ToString("yyyy") + " " + podcastPart.Title));

        // add hosts
        var hosts = from host in podcastPart.Hosts
                    orderby host.Name
                    select host.Name;
        context.Response.Element.Add(new XElement("managingEditor", string.Join(",", hosts.ToArray())));
        context.Response.Element.Add(new XElement("webMaster", string.Join(",", hosts.ToArray())));

        //HACK: hard coded
        context.Response.Element.Add(new XElement("language", "en-US"));

        //HACK: hard coded syndication
        XNamespace syNS = "http://purl.org/rss/1.0/modules/syndication/";
        context.Response.Element.Parent.Add(new XAttribute(XNamespace.Xmlns + "sy", syNS.NamespaceName));

        context.Response.Element.Add(
          new XElement(syNS + "updatePeriod",
          "hourly"));
        context.Response.Element.Add(
          new XElement(syNS + "updateFrequency",
          "1"));

        // itunes
        XNamespace itunesNS = "http://www.itunes.com/dtds/podcast-1.0.dtd";
        context.Response.Element.Parent.Add(new XAttribute(XNamespace.Xmlns + "itunes", itunesNS.NamespaceName));

        context.Response.Element.Add(
          new XElement(itunesNS + "subtitle",
          podcastPart.Description));
        context.Response.Element.Add(
          new XElement(itunesNS + "summary",
          podcastPart.Description));
        context.Response.Element.Add(
          new XElement(itunesNS + "block",
          "no"));
        context.Response.Element.Add(
          new XElement(itunesNS + "author",
          string.Join(",", hosts.ToArray())));
        context.Response.Element.Add(
          new XElement(itunesNS + "explicit",
          podcastPart.Rating == SimpleRatingTypes.NonAdult ? "no" : "yes"));

        var itunesOwner = new XElement(itunesNS + "owner");
        itunesOwner.Add(
          new XElement(itunesNS + "name",
            string.Join(",", hosts.ToArray())));
        //HACK: hard coded
        itunesOwner.Add(
          new XElement(itunesNS + "email",
            "mscloudshow@outlook.com"));
        context.Response.Element.Add(itunesOwner);
        //HACK: hard coded
        context.Response.Element.Add(
          new XElement(itunesNS + "image",
          new XAttribute("href", "http://assets.microsoftcloudshow.com/media/mscloudshow/logos/mscloudshow1040x1040.jpg")));
        //HACK: hard coded
        context.Response.Element.Add(
          new XElement(itunesNS + "keywords",
          "microsoft,sharepoint,azure,cloud,office365,office 365,azure"));
        //HACK: hard coded
        context.Response.Element.Add(new XElement(itunesNS + "category",
          new XAttribute("text", "Technology")));
        var itunesCategory = new XElement(itunesNS + "category",
          new XAttribute("text", "Technology"));
        itunesCategory.Add(new XElement(itunesNS + "category",
          new XAttribute("text", "Tech News")));
        context.Response.Element.Add(itunesCategory);

        // media
        XNamespace mediaNS = "http://search.yahoo.com/mrss/";
        context.Response.Element.Parent.Add(new XAttribute(XNamespace.Xmlns + "media", mediaNS.NamespaceName));

        context.Response.Element.Add(
          new XElement(mediaNS + "description",
          new XAttribute("type", "plain"),
          podcastPart.Description));
        context.Response.Element.Add(new XElement(mediaNS + "copyright", "Copyright " + DateTime.UtcNow.ToString("yyyy") + " " + podcastPart.Title));
        context.Response.Element.Add(
          new XElement(mediaNS + "rating",
          podcastPart.Rating.ToString().ToLower()));
        context.Response.Element.Add(
          new XElement(itunesNS + "credit",
          new XAttribute("role", "owner"),
          string.Join(",", hosts.ToArray())));
        //HACK: hard coded
        context.Response.Element.Add(
          new XElement(mediaNS + "keywords",
          "microsoft,sharepoint,azure,cloud,office365,office 365,azure"));
        //HACK: hard coded
        context.Response.Element.Add(
          new XElement(mediaNS + "thumbnail",
          new XAttribute("url", "http://assets.microsoftcloudshow.com/media/mscloudshow/logos/mscloudshow1040x1040.jpg")));
        //HACK: hard coded
        context.Response.Element.Add(new XElement(mediaNS + "category",
          new XAttribute("scheme", itunesNS.NamespaceName),
          "Technology"));
        //HACK: hard coded
        context.Response.Element.Add(new XElement(mediaNS + "category",
          new XAttribute("scheme", itunesNS.NamespaceName),
          "Technology/Tech Newss"));

      } else {
        context.Builder.AddProperty(context, null, "title", podcastPart.Title);
        context.Builder.AddProperty(context, null, "description", inspector.Description);
        context.Response.Contextualize(requestContext => {
          var urlHelper = new UrlHelper(requestContext);
          context.Builder.AddProperty(context, null, "link", urlHelper.RouteUrl(inspector.Link));
        });
      }
      var items = _contentManager.Query()
        .Where<CommonPartRecord>(x => x.Container == container.Record)
        .OrderByDescending(x => x.CreatedUtc)
        .Slice(0, limit);

      foreach (var item in items) {
        context.Builder.AddItem(context, item);
      }
    }

  }
}