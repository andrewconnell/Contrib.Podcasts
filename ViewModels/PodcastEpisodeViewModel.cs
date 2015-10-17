using System;
using System.Collections.Generic;
using Contrib.Podcasts.Models;

namespace Contrib.Podcasts.ViewModels {
  public class PodcastEpisodeViewModel {
    public int PodcastId { get; set; }
    public string Description { get; set; }
    public int EpisodeNumber { get; set; }

    public string EnclosureUrl { get; set; }
    public string Duration { get; set; }
    public int EnclosureFileSize { get; set; }
    public SimpleRatingTypes Rating { get; set; }

    public IEnumerable<PersonRecord> AvailablePeople { get; set; }
    public IEnumerable<int> Hosts { get; set; }
    public IEnumerable<int> Guests { get; set; }
  }
}