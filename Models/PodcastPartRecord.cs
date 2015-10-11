using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement.Records;
using Orchard.Data.Conventions;

namespace Contrib.Podcasts.Models {
  public class PodcastPartRecord : ContentPartRecord {
    /// <summary>
    /// Podcast description.
    /// </summary>
    [StringLengthMax]
    public virtual string Description { get; set; }

    /// <summary>
    /// Podcast rating.
    /// </summary>
    public virtual SimpleRatingTypes Rating { get; set; }
    
    /// <summary>
    /// Podcast license.
    /// </summary>
    public virtual CreativeCommonsLicenseTypes CreativeCommonsLicense { get; set; }

    /// <summary>
    /// Flag indicating if the show transcripts should be included in the RSS feed.
    /// </summary>
    public virtual bool IncludeTranscriptInFeed { get; set; }

    /// <summary>
    /// Contact email for the podcast.
    /// </summary>
    [Required]
    public virtual string ContactEmail { get; set; }

    /// <summary>
    /// CSV list of keywords to include in the RSS feed.
    /// </summary>
    [StringLengthMax]
    public virtual string Keywords { get; set; }

    /// <summary>
    /// Subtitle of the podcast used for iTunes.
    /// </summary>
    [Required]
    public virtual string Subtitle { get; set; }

    /// <summary>
    /// Summary of the podcast used for iTunes.
    /// </summary>
    [Required]
    [StringLengthMax]
    public virtual string Summary { get; set; }

    /// <summary>
    /// Culture the podcast primarily targets.
    /// </summary>
    [Required]
    public virtual string CultureCode { get; set; }

    /// <summary>
    /// URL of the logo image to include in the RSS feed.
    /// </summary>
    [Required]
    public virtual string LogoImageUrl { get; set; }

    /// <summary>
    /// Frequency the RSS feed should be checked for new episodes.
    /// </summary>
    [Required]
    public virtual string UpdateFrequency { get; set; }

    /// <summary>
    /// How often the frequency should be checked.
    /// </summary>
    [Required]
    public virtual int UpdatePeriod { get; set; }

    public virtual string Categories { get; set; }

    public PodcastPartRecord() {
      Hosts = new List<PodcastHostRecord>();
    }

    public virtual IList<PodcastHostRecord> Hosts { get; set; }
  }
}