using System;
using System.Collections.Generic;
using Orchard.ContentManagement.Records;

namespace Contrib.Podcasts.Models {
  public class PodcastEpisodePartRecord : ContentPartRecord {

    /// <summary>
    /// Unique ID of the podcast.
    /// </summary>
    public virtual int PodcastId { get; set; }

    /// <summary>
    /// Description of the episode.
    /// </summary>
    public virtual string Description { get; set; }
    /// <summary>
    /// Episode number.
    /// </summary>
    public virtual int EpisodeNumber { get; set; }

    /// <summary>
    /// Absolute URL to the location of the episode MP3.
    /// </summary>
    public virtual string EnclosureUrl { get; set; }

    /// <summary>
    /// Length of the episode in minutes and seconds
    /// </summary>
    /// <example>32:54</example>
    public virtual string Duration { get; set; }

    /// <summary>
    /// Filesize of the episode in bytes.
    /// </summary>
    public virtual decimal EnclosureFilesize { get; set; }

    public PodcastEpisodePartRecord() {
      PodcastPeople = new List<EpisodePersonRecord>();
    }

    public virtual IList<EpisodePersonRecord> PodcastPeople { get; set; }

    /// <summary>
    /// Rating for the episode.
    /// </summary>
    public virtual SimpleRatingTypes Rating { get; set; }

    // TODO: need a way to add tags for this entry
  }
}