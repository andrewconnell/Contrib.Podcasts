using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Contrib.Podcasts.Models;

namespace Contrib.Podcasts.ViewModels {

  public class PodcastViewModel {
    public string Description { get; set; }
    public IEnumerable<int> Hosts { get; set; }
    public IEnumerable<PersonRecord> AvailablePeople { get; set; }
    public CreativeCommonsLicenseTypes License { get; set; }
    public SimpleRatingTypes Rating { get; set; }
    public bool IncludeEpisodeTranscriptInFeed { get; set; }
    public string ContactEmail { get; set; }
    public string Keywords { get; set; }
    public string Subtitle { get; set; }
    public string Summary { get; set; }
    public string CultureCode { get; set; }
    public string LogoImageUrl { get; set; }
    public string UpdateFrequency { get; set; }
    public int UpdatePeriod { get; set; }
    public string PodcastCategories { get; set; }
  }

}