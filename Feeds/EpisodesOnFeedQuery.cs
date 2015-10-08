using System;
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
        var link = new XElement("link");

        context.Response.Element.SetElementValue("title", podcastPart.Title);
        context.Response.Element.Add(link);
        context.Response.Element.SetElementValue("description", podcastPart.Description);
        context.Response.Contextualize(requestContext => {
          var urlHelper = new UrlHelper(requestContext);
          var uriBuilder = new UriBuilder(urlHelper.RequestContext.HttpContext.Request.ToRootUrlString()) { Path = urlHelper.RouteUrl(inspector.Link) };
          link.Add(uriBuilder.Uri.OriginalString);
        });
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