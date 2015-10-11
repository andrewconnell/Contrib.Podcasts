using System.Collections.Generic;
using System.Linq;
using Contrib.Podcasts.Models;
using Contrib.Podcasts.Services;
using Contrib.Podcasts.ViewModels;
using Orchard.ContentManagement.Drivers;
using Orchard.Data;

namespace Contrib.Podcasts.Drivers {
  public class PodcastPartDriver : ContentPartDriver<PodcastPart> {
    private readonly IRepository<PersonRecord> _personRepository;
    private readonly IPodcastService _podcastService;

    public PodcastPartDriver(IRepository<PersonRecord> personRepository, IPodcastService podcastService) {
      _personRepository = personRepository;
      _podcastService = podcastService;
    }

    protected override string Prefix {
      get { return "Podcast"; }
    }

    protected override DriverResult Display(PodcastPart part, string displayType, dynamic shapeHelper) {
      return Combined(
        ContentShape("Parts_Podcasts_Podcast",
          () => shapeHelper.Parts_Podcasts_Podcast(PodcastPart: part)),
        ContentShape("Parts_Podcasts_Podcast_Description",
          () => shapeHelper.Parts_Podcasts_Podcast_Description(Description: part.Description)),
        ContentShape("Parts_Podcasts_Podcast_Manage",
          () => shapeHelper.Parts_Podcasts_Podcast_Manage()),
        ContentShape("Parts_Podcasts_Podcast_SummaryAdmin",
          () => shapeHelper.Parts_Podcasts_Podcast_SummaryAdmin())
        );
    }

    /// <summary>
    /// GET method used for loading the editor.
    /// </summary>
    protected override DriverResult Editor(PodcastPart part, dynamic shapeHelper) {
      // get default shapes
      var shapes = new List<DriverResult> {
        ContentShape("Parts_Podcasts_Podcast_Fields",
                     () => shapeHelper.EditorTemplate(TemplateName: "Parts.Podcasts.Podcast.Fields", Model: BuildViewModel(part), Prefix: Prefix))
      };

      // if the ID of current podcast isn't zero, add a delete button
      if (part.Id > 0)
        shapes.Add(ContentShape("Podcast_DeleteButton", deleteButton => deleteButton));

      return Combined(shapes.ToArray());
    }

    /// <summary>
    /// POST method used for persisting the data
    /// </summary>
    protected override DriverResult Editor(PodcastPart part, Orchard.ContentManagement.IUpdateModel updater, dynamic shapeHelper) {
      var viewModel = new PodcastViewModel();
      updater.TryUpdateModel(viewModel, Prefix, null, new[] { "AvailablePeople" });
      _podcastService.Update(viewModel, part);
      return Editor(part, shapeHelper);
    }

    private PodcastViewModel BuildViewModel(PodcastPart part) {
      return new PodcastViewModel {
        Description = part.Description,
        AvailablePeople = _personRepository.Table.OrderBy(p => p.Name).ToList(),
        Hosts = part.Hosts.Select(host => host.Id).ToList(),
        IncludeEpisodeTranscriptInFeed = part.IncludeTranscriptInFeed,
        License = part.CreativeCommonsLicense,
        Rating = part.Rating,
        ContactEmail = part.ContactEmail,
        Keywords = part.Keywords,
        Subtitle = part.Subtitle,
        Summary = part.Summary,
        CultureCode = part.CultureCode,
        LogoImageUrl = part.LogoImageUrl,
        UpdateFrequency = part.UpdateFrequency,
        UpdatePeriod = part.UpdatePeriod,
        PodcastCategories = part.PodcastCategories
      };
    }
  }
}