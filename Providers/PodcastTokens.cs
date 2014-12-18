using Contrib.Podcasts.Models;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Tokens;

namespace Contrib.Podcasts.Providers {
  public class PodcastTokens : ITokenProvider {
    private readonly IContentManager _contentManager;

    public PodcastTokens() {
      T = NullLocalizer.Instance;
    }

    public Localizer T { get; set; }

    public void Describe(DescribeContext context) {
      context.For("Content", T("Content Items"), T("Content Items"))
        .Token("PodcastEpisode", T("PodcastEpisode"), T("Provides access to the PodcastEpisode part."), "PodcastEpisode");
      context.For("PodcastEpisode", T("Podcast Episode Items"), T("Tokens for podcast text strings"))
        .Token("EpisodeNumber", T("Episode Number"), T("Includes the episode number."))
        .Token("FormatEpisodeNumber", T("Episodenumber"), T("Format Specifier For EpisodeNumber"));
    }

    public void Evaluate(EvaluateContext context) {
      context.For<IContent>("Content")
        .Token("PodcastEpisode", content => content.As<PodcastEpisodePart>())
        .Chain("PodcastEpisode", "PodcastEpisode", content => content.As<PodcastEpisodePart>());
      context.For<PodcastEpisodePart>("PodcastEpisode")
        .Token("EpisodeNumber", content => content.EpisodeNumber)
        .Token("FormatEpisodeNumber", content => string.Format("{0:000}", content.EpisodeNumber));
    }
  }
}