using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Orchard.ContentManagement;
using Orchard.Core.Title.Models;

namespace Contrib.Podcasts.Models {
  public class PodcastPart : ContentPart<PodcastPartRecord> {

    public string Title {
      get { return this.As<TitlePart>().Title; }
      set { this.As<TitlePart>().Title = value; }
    }

    public string Description {
      get { return Record.Description; }
      set { Record.Description = value; }
    }

    public IEnumerable<PersonRecord> Hosts {
      get { return Record.Hosts.ToList().Select(host => host.PersonRecord); }
    }

    public SimpleRatingTypes Rating {
      get { return Record.Rating; }
      set { Record.Rating = value; }
    }

    public CreativeCommonsLicenseTypes CreativeCommonsLicense {
      get { return Record.CreativeCommonsLicense; }
      set { Record.CreativeCommonsLicense = value; }
    }

    public bool IncludeTranscriptInFeed {
      get { return Record.IncludeTranscriptInFeed; }
      set { Record.IncludeTranscriptInFeed = value; }
    }

    public string ContactEmail {
      get { return Record.ContactEmail; }
      set { Record.ContactEmail = value; }
    }

    public string Keywords {
      get { return Record.Keywords; }
      set { Record.Keywords = value; }
    }

    public string Subtitle {
      get { return Record.Subtitle; }
      set { Record.Subtitle = value; }
    }

    public string Summary {
      get { return Record.Summary; }
      set { Record.Summary = value; }
    }

    public string CultureCode {
      get { return Record.CultureCode; }
      set { Record.CultureCode = value; }
    }

    public string LogoImageUrl {
      get { return Record.LogoImageUrl; }
      set { Record.LogoImageUrl = value; }
    }

    public string UpdateFrequency {
      get { return Record.UpdateFrequency; }
      set { Record.UpdateFrequency = value; }
    }

    public int UpdatePeriod {
      get { return Record.UpdatePeriod; }
      set { Record.UpdatePeriod = value; }
    }

    public string PodcastCategories {
      get { return Record.Categories; }
      set { Record.Categories = value; }
    }
  }

}