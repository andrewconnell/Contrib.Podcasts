using System.Web;
using Contrib.Podcasts.Models;
using Contrib.Podcasts.Services;
using JetBrains.Annotations;
using Orchard.ContentManagement;
using Orchard.Core.Feeds;
using Orchard.Core.Feeds.Models;
using Orchard.Core.Feeds.StandardBuilders;
using Orchard.Localization;
using Orchard.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Routing;
using System.Xml.Linq;
using System.Diagnostics;
using Orchard.Caching;

namespace Contrib.Podcasts.Feeds {
  [UsedImplicitly]
  public class PodcastEpisodeFeedItemBuilder : IFeedItemBuilder {
    private readonly IContentManager _contentManager;
    private readonly IEnumerable<IHtmlFilter> _htmlFilters;
    private readonly IPodcastService _podcastService;
    private readonly IPodcastEpisodeService _podcastEpisodeService;
    private readonly ICacheManager _cacheManager;
    private readonly IClock _clock;
    private readonly ISignals _signals;
    private readonly RouteCollection _routes;

    public PodcastEpisodeFeedItemBuilder(IContentManager contentManager,
                                          IPodcastService podcastService,
                                          IPodcastEpisodeService podcastEpisodeService,
                                          ICacheManager cacheManager,
                                          IClock clock,
                                          ISignals signals,
                                          RouteCollection routes,
                                          IEnumerable<IHtmlFilter> htmlFilters) {
      _contentManager = contentManager;
      _podcastService = podcastService;
      _podcastEpisodeService = podcastEpisodeService;
      _cacheManager = cacheManager;
      _routes = routes;
      _clock = clock;
      _signals = signals;
      _htmlFilters = htmlFilters;
      T = NullLocalizer.Instance;
    }
    public Localizer T { get; set; }

    public void Populate(FeedContext context) {
      Debug.WriteLine("Podcast.PodcastEpisodeFeedItemBuilder.Populate.Start");

      var containerIdValue = context.ValueProvider.GetValue("containerid");
      var containerId = (int)containerIdValue.ConvertTo(typeof(int));
      var container = _contentManager.Get(containerId);

      if (container == null) {
        return;
      }
      if (container.ContentType != "Podcast") {
        return;
      }

      PodcastPart podcastPart = _podcastService.Get(containerId).As<PodcastPart>();

      var feedItems = context.Response.Items.OfType<FeedItem<ContentItem>>();
      // update the items
      var timerStart = DateTime.UtcNow;
      foreach (var feedItem in feedItems) {
        //Debug.WriteLine("Contrib.Podcast|Begin add item to RSS feed");
        var inspector = new ItemInspector(feedItem.Item, _contentManager.GetItemMetadata(feedItem.Item), _htmlFilters);
        if (context.Format != "rss") {
          return;
        }

        // get reference to the feed XML element
        var feedElement = feedItem.Element;

        // update the element's XML
        feedElement = UpdateFeedItem(podcastPart, inspector, feedItem.Item.Id, feedElement);

        Debug.WriteLine("Contrib.Podcast|ElapsedTime: " + DateTime.UtcNow.Subtract(timerStart).TotalSeconds);
      }

      Debug.WriteLine("Contrib.Podcast|ElapsedTime: " + DateTime.UtcNow.Subtract(timerStart).TotalSeconds);
      Debug.WriteLine("Podcast.PodcastEpisodeFeedItemBuilder.Populate.End");
    }

    /// <summary>
    /// Takes a default RSS item from Orchard and updates it with podcast episode details.
    /// </summary>
    /// <param name="podcastPart">The podcast object.</param>
    /// <param name="inspector">Orchard item inspector</param>
    /// <param name="feedItemId">Id of feed item to update</param>
    /// <param name="baseFeedElement">Orchard RSS feed item</param>
    /// <returns></returns>
    private XElement UpdateFeedItem(PodcastPart podcastPart, ItemInspector inspector, int feedItemId, XElement baseFeedElement) {
      XNamespace itunesNS = "http://www.itunes.com/dtds/podcast-1.0.dtd";
      XNamespace dcNS = "http://purl.org/dc/elements/1.1/";

      // setup item to update
      var updatedFeedElement = baseFeedElement;


      // get the feed item from the database (but try cache first)
      var cacheKeyPrefix = string.Format("Contrib.Podcasts[{0}].FeedItem[{1}]", podcastPart.Id, feedItemId);
      Debug.WriteLine("Contrib.Podcast||CacheKey= " + cacheKeyPrefix);
      var podcastEpisodesDetail = _cacheManager.Get(string.Concat(cacheKeyPrefix, ".EpisodeDetail"),
        ctx => {
          // ... expire this item after 6hrs
          ctx.Monitor(_clock.When(TimeSpan.FromHours(6)));
          // ... or when a signal is fired
          ctx.Monitor(_signals.When(string.Concat(cacheKeyPrefix, "_Evict")));

          // get the podcast episode detail core orchard item
          return _podcastEpisodeService.Get(feedItemId);
        });

      var episodePart = _cacheManager.Get(string.Concat(cacheKeyPrefix, ".EpisodePart"),
        ctx => {
          // ... expire this item after 6hrs
          ctx.Monitor(_clock.When(TimeSpan.FromHours(6)));
          // ... or when a signal is fired
          ctx.Monitor(_signals.When(string.Concat(cacheKeyPrefix, "_Evict")));

          // get the podcast episode part
          dynamic episodeType = _contentManager.Query().ForType("PodcastEpisode").List().First(x => x.Record.Id == podcastEpisodesDetail.Id);
          return episodeType.PodcastEpisodePart;
        });


      // set title
      Debug.WriteLine("Contrib.Podcast||" + string.Format("{0:000} | {1}", podcastEpisodesDetail.EpisodeNumber, podcastEpisodesDetail.Title));
      updatedFeedElement.SetElementValue("title", string.Format("{0:000} | {1}", podcastEpisodesDetail.EpisodeNumber, podcastEpisodesDetail.Title));

      if (inspector.PublishedUtc != null) {
        updatedFeedElement.SetElementValue("pubDate", inspector.PublishedUtc.Value.ToString("r"));
      }

      if (podcastEpisodesDetail.EnclosureUrl != null) {
        updatedFeedElement.Add(new XElement("enclosure",
          new XAttribute("url", podcastEpisodesDetail.EnclosureUrl),
          new XAttribute("length", Convert.ToInt32(podcastEpisodesDetail.EnclosureFilesize)),
          new XAttribute("type", "audio/mpeg")));
      }
      if (episodePart.Description != null) {
        var description = HttpUtility.HtmlEncode(episodePart.Description);
        updatedFeedElement.SetElementValue("description", description);
        updatedFeedElement.Add(new XElement(itunesNS + "subtitle", description));
        updatedFeedElement.Add(new XElement(itunesNS + "summary", description));
      }
      var hosts = from host in podcastEpisodesDetail.Hosts
                  orderby host.Name
                  select host.Name;
      var guests = from guest in podcastEpisodesDetail.Guests
                   orderby guest.Name
                   select guest.Name;
      var hostList = string.Join(",", hosts.ToArray());
      var participantList = string.Join(",", (hosts.Concat(guests)).ToArray());

      updatedFeedElement.Add(new XElement(itunesNS + "duration", podcastEpisodesDetail.Duration));
      updatedFeedElement.Add(new XElement(itunesNS + "keywords", "Episodes"));
      updatedFeedElement.Add(new XElement(itunesNS + "author", participantList));
      updatedFeedElement.Add(new XElement(itunesNS + "explicit", podcastPart.Rating == SimpleRatingTypes.NonAdult ? "no" : "yes"));
      updatedFeedElement.Add(new XElement(itunesNS + "block", "no"));
      updatedFeedElement.Add(new XElement(dcNS + "creator", hostList));

      return updatedFeedElement;
    }
  }
}