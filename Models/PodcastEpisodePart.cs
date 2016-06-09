using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.ContentManagement.Utilities;
using Orchard.Core.Common.Models;
using Orchard.Core.Title.Models;
using Orchard.Security;

namespace Contrib.Podcasts.Models {
  public class PodcastEpisodePart : ContentPart<PodcastEpisodePartRecord> {

    [Required]
    public int PodcastId {
      get { return Record.PodcastId; }
      set { Record.PodcastId = value; }
    }

    [Required]
    public string Description {
      get { return Record.Description; }
      set { Record.Description = value; }
    }

    [Required]
    public int EpisodeNumber {
      get { return Record.EpisodeNumber; }
      set { Record.EpisodeNumber = value; }
    }

    [Required]
    public string Title {
      get { return this.As<TitlePart>().Title; }
      set { this.As<TitlePart>().Title = value; }
    }

    public string EnclosureUrl {
      get { return Record.EnclosureUrl; }
      set { Record.EnclosureUrl = value; }
    }

    public string Duration {
      get { return Record.Duration; }
      set { Record.Duration = value; }
    }

    public decimal EnclosureFilesize {
      get { return Record.EnclosureFilesize; }
      set { Record.EnclosureFilesize = value; }
    }

    public SimpleRatingTypes Rating {
      get { return Record.Rating; }
      set { Record.Rating = value; }
    }

    public string EpisodeImageUrl {
      get { return Record.EpisodeImageUrl; }
      set { Record.EpisodeImageUrl = value; }
    }

    public IEnumerable<PersonRecord> Hosts {
      get { return Record.PodcastPeople.ToList().Where(p => p.IsHost == true).Select(p => p.PersonRecord); }
    }

    public IEnumerable<PersonRecord> Guests {
      get { return Record.PodcastPeople.ToList().Where(p => p.IsHost == false).Select(p => p.PersonRecord); }
    }

    public PodcastPart PodcastPart {
      get { return this.As<ICommonPart>().Container.As<PodcastPart>(); }
      set { this.As<ICommonPart>().Container = value; }
    }

    public IUser Creator {
      get { return this.As<ICommonPart>().Owner; }
      set { this.As<ICommonPart>().Owner = value; }
    }

    public bool IsPublished {
      get { return ContentItem.VersionRecord != null && ContentItem.VersionRecord.Published; }
    }

    public bool HasDraft {
      get {
        return (
                   (ContentItem.VersionRecord != null) && (
                       (ContentItem.VersionRecord.Published == false) ||
                       (ContentItem.VersionRecord.Published && ContentItem.VersionRecord.Latest == false)));
      }
    }

    public bool HasPublished {
      get {
        return IsPublished || ContentItem.ContentManager.Get(Id, VersionOptions.Published) != null;
      }
    }

    public DateTime? PublishedUtc {
      get { return this.As<ICommonPart>().PublishedUtc; }
    }
  }
}