using System.Security.Cryptography;
using System.Web;
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

      XNamespace itunesNS = "http://www.itunes.com/dtds/podcast-1.0.dtd";
      XNamespace dcNS = "http://purl.org/dc/elements/1.1/";

      // update the items
      foreach (var feedItem in context.Response.Items.OfType<FeedItem<ContentItem>>()) {
        var inspector = new ItemInspector(feedItem.Item, _contentManager.GetItemMetadata(feedItem.Item), _htmlFilters);
        if (context.Format != "rss") {
          return;
        }

        var podcastEpisodesDetail = _podcastEpisodeService.Get(feedItem.Item.Id);
        dynamic episodeType = _contentManager.Query().ForType("PodcastEpisode").List().First(x => x.Record.Id == podcastEpisodesDetail.Id);
        var episodePart = episodeType.PodcastEpisodePart;

        if (inspector.PublishedUtc != null) {
          feedItem.Element.SetElementValue("pubDate", inspector.PublishedUtc.Value.ToString("r"));
        }

        if (podcastEpisodesDetail.EnclosureUrl != null) {
          feedItem.Element.Add(new XElement("enclosure",
            new XAttribute("url", podcastEpisodesDetail.EnclosureUrl),
            new XAttribute("length", podcastEpisodesDetail.EnclosureFilesize),
            new XAttribute("type", "audio/mpeg")));
        }
        if (episodePart.ShowNotes.Value != null) {
          var shownotes = HttpUtility.HtmlEncode(episodePart.ShowNotes.Value);
          feedItem.Element.SetElementValue("description", shownotes);
          feedItem.Element.Add(new XElement(itunesNS + "subtitle", shownotes));
          feedItem.Element.Add(new XElement(itunesNS + "summary", shownotes));
        }
        var hosts = from host in podcastEpisodesDetail.Hosts
                    orderby host.Name
                    select host.Name;
        var guests = from guest in podcastEpisodesDetail.Guests
                     orderby guest.Name
                     select guest.Name;
        feedItem.Element.Add(new XElement(itunesNS + "duration", podcastEpisodesDetail.Duration));
        feedItem.Element.Add(new XElement(itunesNS + "keywords", "Episodes"));
        feedItem.Element.Add(new XElement(itunesNS + "author", string.Join(",", (hosts.Concat(guests)).ToArray())));
        feedItem.Element.Add(new XElement(itunesNS + "explicit", podcastPart.Rating == SimpleRatingTypes.NonAdult ? "no" : "yes"));
        feedItem.Element.Add(new XElement(itunesNS + "block", "no"));
        feedItem.Element.Add(new XElement(dcNS + "creator", string.Join(",", hosts.ToArray())));
      }

    }

  }
}