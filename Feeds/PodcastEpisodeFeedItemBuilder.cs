using Contrib.Podcasts.Models;
using Contrib.Podcasts.Services;
using JetBrains.Annotations;
using Orchard;
using Orchard.Caching;
using Orchard.ContentManagement;
using Orchard.Core.Feeds;
using Orchard.Core.Feeds.Models;
using Orchard.Core.Feeds.StandardBuilders;
using Orchard.Localization;
using Orchard.Services;
using Orchard.Settings;
using Orchard.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;
using System.Xml.Linq;
namespace Contrib.Podcasts.Feeds {
  [UsedImplicitly]
  public class PodcastEpisodeFeedItemBuilder : IFeedItemBuilder {
    private readonly IContentManager _contentManager;
    private readonly IEnumerable<IHtmlFilter> _htmlFilters;
    private readonly IPodcastService _podcastService;
    private readonly IPodcastEpisodeService _podcastEpisodeService;
    private readonly RouteCollection _routes;

    public PodcastEpisodeFeedItemBuilder(IContentManager contentManager, IPodcastService podcastService, IPodcastEpisodeService podcastEpisodeService, RouteCollection routes, IEnumerable<IHtmlFilter> htmlFilters) {
      _contentManager = contentManager;
      _podcastService = podcastService;
      _podcastEpisodeService = podcastEpisodeService;
      _routes = routes;
      _htmlFilters = htmlFilters;
      T = NullLocalizer.Instance;
    }
    public Localizer T { get; set; }

    public void Populate(FeedContext context) {
      var containerIdValue = context.ValueProvider.GetValue("containerid");
      var containerId = (int)containerIdValue.ConvertTo(typeof(int));
      var container = _contentManager.Get(containerId);
      if (container == null) {
        return;
      }
      PodcastPart podcastPart = _podcastService.Get(containerId).As<PodcastPart>();
      var podcastEpisodes = _podcastEpisodeService.Get(podcastPart);
      XNamespace itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
      XNamespace dc = "http://purl.org/dc/elements/1.1/";
      foreach (var feedItem in context.Response.Items.OfType<FeedItem<ContentItem>>()) {
        var inspector = new ItemInspector(
        feedItem.Item,
        _contentManager.GetItemMetadata(feedItem.Item),
        _htmlFilters);
        if (context.Format == "rss") {

          var podcastEpisodesDetail = _podcastEpisodeService.Get(feedItem.Item.Id);
          var PodcastEpisodeNumber = string.Format("{0:000}", podcastEpisodesDetail.EpisodeNumber);
          dynamic episodeType = _contentManager.Query().ForType("PodcastEpisode").List().First(x => x.Record.Id == podcastEpisodesDetail.Id);
          var episodePart = episodeType.PodcastEpisodePart;

          var link = new XElement("link");
          var guid = new XElement("guid", new XAttribute("isPermaLink", "false"));
          var EpisodeNumber = new XElement("EpisodeNumber");
          var category = new XElement("category");
          context.Response.Contextualize(requestContext => {
            var urlHelper = new UrlHelper(requestContext, _routes);
            var uriBuilder = new UriBuilder(urlHelper.RequestContext.HttpContext.Request.ToRootUrlString()) { Path = urlHelper.RouteUrl(inspector.Link) };
            link.Add(uriBuilder.Uri.OriginalString);
            guid.Add(uriBuilder.Uri.OriginalString);
          });

          feedItem.Element.SetElementValue("title", "Episode " + PodcastEpisodeNumber + " | " + podcastEpisodesDetail.Title);
          feedItem.Element.Add(link);
          if (inspector.PublishedUtc != null) {
            feedItem.Element.SetElementValue("pubDate", inspector.PublishedUtc.Value.ToString("r"));
          }
          feedItem.Element.Add(guid);

          if (podcastEpisodesDetail.EnclosureUrl != null) {
            var EnclouserUrl = new XElement("enclosure", new XAttribute("url", podcastEpisodesDetail.EnclosureUrl), new XAttribute("length", podcastEpisodesDetail.EnclosureFilesize), new XAttribute("type", "audio/mpeg"));
            feedItem.Element.Add(EnclouserUrl);
          }
          if (episodePart.ShowNotes.Value != null) {
            var shownotes = episodePart.ShowNotes.Value;
            feedItem.Element.SetElementValue("description", shownotes);
            var ItuneSubtitle = new XElement(itunes + "subtitle", new XAttribute(XNamespace.Xmlns + "itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd"), shownotes);
            feedItem.Element.Add(ItuneSubtitle);
            var ItuneSummary = new XElement(itunes + "summary", new XAttribute(XNamespace.Xmlns + "itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd"), shownotes);
            feedItem.Element.Add(ItuneSummary);
          }
          var Ituneduration = new XElement(itunes + "duration", new XAttribute(XNamespace.Xmlns + "itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd"), podcastEpisodesDetail.Duration);
          var ItuneKeywords = new XElement(itunes + "keywords", new XAttribute(XNamespace.Xmlns + "itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd"), "Episodes");
          var Ituneauthor = new XElement(itunes + "author", new XAttribute(XNamespace.Xmlns + "itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd"), "Andrew Connell and Chris Johnson");
          var itunesexplicit = new XElement(itunes + "explicit", new XAttribute(XNamespace.Xmlns + "itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd"), "no");
          var itunesblock = new XElement(itunes + "block", new XAttribute(XNamespace.Xmlns + "itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd"), "no");
          feedItem.Element.Add(Ituneduration);
          feedItem.Element.Add(ItuneKeywords);
          feedItem.Element.Add(Ituneauthor);
          feedItem.Element.Add(itunesexplicit);
          feedItem.Element.Add(itunesblock);
          var dccreator = new XElement(dc + "creator", new XAttribute(XNamespace.Xmlns + "dc", "http://purl.org/dc/elements/1.1/"), "no");
          feedItem.Element.Add(dccreator);
          feedItem.Element.SetElementValue("EpisodeNumber", PodcastEpisodeNumber);
        }

      }




    }

  }
  //}
}


