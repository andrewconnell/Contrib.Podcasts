using System;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using JetBrains.Annotations;
using Contrib.Podcasts.Models;
using Newtonsoft.Json;
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
      // if the container is not a podcast, don't return this provider
      if (container.ContentType != "Podcast") {
        return null;
      }

      // otherwise, return this provider with a high priority
      return new FeedQueryMatch { FeedQuery = this, Priority = 0 };
    }
    public void Execute(FeedContext context) {
      var containerIdValue = context.ValueProvider.GetValue("containerid");
      if (containerIdValue == null)
        return;

      var limitValue = context.ValueProvider.GetValue("limit");
      var limit = 50;
      if (limitValue != null)
        limit = (int)limitValue.ConvertTo(typeof(int));

      var containerId = (int)containerIdValue.ConvertTo(typeof(int));
      var container = _contentManager.Get(containerId);

      if (container == null) {
        return;
      }
      if (container.ContentType != "Podcast") {
        return;
      }

      PodcastPart podcastPart = _podcastService.Get(containerId).As<PodcastPart>();
      var inspector = new ItemInspector(container, _contentManager.GetItemMetadata(container), _htmlFilters);
      if (context.Format == "rss") {
        // add namespace
        XNamespace dcNS = "http://purl.org/dc/elements/1.1/";
        context.Response.Element.Parent.Add(new XAttribute(XNamespace.Xmlns + "dc", dcNS.NamespaceName));
        XNamespace ccNS = "http://backend.userland.com/creativeCommonsRssModule";
        context.Response.Element.Parent.Add(new XAttribute(XNamespace.Xmlns + "creativeCommons", ccNS.NamespaceName));

        var podcastLink = new XElement("link");
        var imageLink = new XElement("link");

        context.Response.Element.SetElementValue("title", podcastPart.Title);
        context.Response.Element.Add(podcastLink);
        context.Response.Element.SetElementValue("description", podcastPart.Description);
        context.Response.Element.Add(new XElement("lastBuildDate", DateTime.UtcNow.ToString("r")));
        context.Response.Element.Add(new XElement("copyright", "Copyright " + DateTime.UtcNow.ToString("yyyy") + " " + podcastPart.Title));
        context.Response.Element.Add(new XElement("ttl", "1440"));

        // add image
        if (podcastPart.LogoImageUrl != null) {
          var podcastImage = new XElement("image");
          podcastImage.Add(new XElement("title", podcastPart.Title));
          podcastImage.Add(imageLink);
          podcastImage.Add(new XElement("url", podcastPart.LogoImageUrl));
          context.Response.Element.Add(podcastImage);
        }

        context.Response.Contextualize(requestContext => {
          var urlHelper = new UrlHelper(requestContext);
          var uriBuilder = new UriBuilder(urlHelper.RequestContext.HttpContext.Request.ToRootUrlString()) { Path = urlHelper.RouteUrl(inspector.Link) };
          podcastLink.Add(uriBuilder.Uri.OriginalString);
          imageLink.Add(uriBuilder.Uri.OriginalString);
        });

        // add hosts
        var hosts = from host in podcastPart.Hosts
                    orderby host.Name
                    select host.Name;
        context.Response.Element.Add(new XElement("managingEditor", string.Join(",", hosts.ToArray())));
        context.Response.Element.Add(new XElement("webMaster", string.Join(",", hosts.ToArray())));
        context.Response.Element.Add(new XElement("language", podcastPart.CultureCode));

        // syndication
        XNamespace syNS = "http://purl.org/rss/1.0/modules/syndication/";
        context.Response.Element.Parent.Add(new XAttribute(XNamespace.Xmlns + "sy", syNS.NamespaceName));

        context.Response.Element.Add(
          new XElement(syNS + "updatePeriod",
          podcastPart.UpdateFrequency));
        context.Response.Element.Add(
          new XElement(syNS + "updateFrequency",
          podcastPart.UpdatePeriod));

        // itunes
        XNamespace itunesNS = "http://www.itunes.com/dtds/podcast-1.0.dtd";
        context.Response.Element.Parent.Add(new XAttribute(XNamespace.Xmlns + "itunes", itunesNS.NamespaceName));

        context.Response.Element.Add(
          new XElement(itunesNS + "subtitle",
          podcastPart.Subtitle));
        context.Response.Element.Add(
          new XElement(itunesNS + "summary",
          podcastPart.Summary));
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
        itunesOwner.Add(
          new XElement(itunesNS + "email",
            podcastPart.ContactEmail));
        context.Response.Element.Add(itunesOwner);
        context.Response.Element.Add(
          new XElement(itunesNS + "image",
          new XAttribute("href", podcastPart.LogoImageUrl)));
        context.Response.Element.Add(
          new XElement(itunesNS + "keywords",
          podcastPart.Keywords));

        // categories
        try {
          var categories = JsonConvert.DeserializeObject<PodcastCategories>(podcastPart.PodcastCategories);
          foreach (var category in categories.Categories) {
            var podcastCategory = new XElement(itunesNS + "category",
              new XAttribute("text", category.Title));
            // check if it has a sub category
            if (category.Subcategory != null) {
              podcastCategory.Add(new XElement(itunesNS + "category",
                new XAttribute("text", category.Subcategory)));
            }
            // add it
            context.Response.Element.Add(podcastCategory);
          }
        } catch { }

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
          new XElement(mediaNS + "credit",
          new XAttribute("role", "owner"),
          string.Join(",", hosts.ToArray())));
        context.Response.Element.Add(
          new XElement(mediaNS + "keywords",
          podcastPart.Keywords));
        context.Response.Element.Add(
          new XElement(mediaNS + "thumbnail",
          new XAttribute("url", podcastPart.LogoImageUrl)));
        // categories
        try {
          var categories = JsonConvert.DeserializeObject<PodcastCategories>(podcastPart.PodcastCategories);
          foreach (var category in categories.Categories) {
            var categoryValue = category.Title;
            if (category.Subcategory != null)
              categoryValue = categoryValue + "/" + category.Subcategory;

            context.Response.Element.Add(new XElement(mediaNS + "category",
              new XAttribute("scheme", itunesNS.NamespaceName),
              categoryValue));
          }
        } catch { }

        // creative commons
        var license = "";
        switch (podcastPart.CreativeCommonsLicense) {
          case CreativeCommonsLicenseTypes.Attribution:
            license = "http://creativecommons.org/licenses/by/4.0";
            break;
          case CreativeCommonsLicenseTypes.AttributionShareAlike:
            license = "http://creativecommons.org/licenses/by-sa/4.0";
            break;
          case CreativeCommonsLicenseTypes.AttributionNoDerivs:
            license = "http://creativecommons.org/licenses/by-nd/4.0";
            break;
          case CreativeCommonsLicenseTypes.AttributionNonCommercial:
            license = "http://creativecommons.org/licenses/by-nc/4.0";
            break;
          case CreativeCommonsLicenseTypes.AttributionNonCommercialShareAlike:
            license = "http://creativecommons.org/licenses/by-nc-sa/4.0";
            break;
          case CreativeCommonsLicenseTypes.AttributionNonCommercialNoDerivs:
            license = "http://creativecommons.org/licenses/by-nc-nd/4.0";
            break;
        }
        // add the license
        if (license != "") {
          context.Response.Element.Add(new XElement(ccNS + "license", license));
        }
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