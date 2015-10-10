using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Contrib.Podcasts.Models;
using Contrib.Podcasts.ViewModels;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Settings;
using Orchard.UI.Admin;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;

namespace Contrib.Podcasts.Controllers {
  [ValidateInput(true)]
  public class PersonAdminController : Controller {
    private readonly IOrchardServices _orchardServices;
    private readonly ISiteService _siteService;
    private readonly IRepository<PersonRecord> _personRepository;

    public PersonAdminController(IOrchardServices orchardServices, ISiteService siteService, IRepository<PersonRecord> personRepository, IShapeFactory shapeFactory) {
      _orchardServices = orchardServices;
      _siteService = siteService;
      _personRepository = personRepository;
      Shape = shapeFactory;
    }

    public Localizer T { get; set; }
    public dynamic Shape { get; set; }

    /// <summary>
    /// MVC action for listing all people in the database.
    /// </summary>
    [HttpGet, Admin]
    public ActionResult Index(PagerParameters pagerParameters) {
      // create a pager control to list the people
      Pager pager = new Pager(_siteService.GetSiteSettings(), pagerParameters);

      var personCount = _personRepository.Table.Count();
      var people = _personRepository.Table
        .OrderBy(p => p.Name)
        .Skip((pager.Page - 1) * pager.PageSize)
        .Take(pager.PageSize);
      var pagerShape = Shape.Pager(pager).TotalItemCount(personCount);

      var viewModel = new PeopleViewModel {
        Pager = pagerShape,
        People = people
      };

      return View(viewModel);
    }

    /// <summary>
    /// Action for displaying the form to create a new person.
    /// </summary>
    [HttpGet, Admin]
    public ActionResult Create() {
      return View(new PersonViewModel());
    }

    /// <summary>
    /// Action for POSTing the create form.
    /// </summary>
    [HttpPost, ActionName("Create"), Admin]
    public ActionResult CreatePOST(PersonViewModel viewModel) {

      if (!ModelState.IsValid) {
        return View(viewModel);
      }
      _personRepository.Create(new PersonRecord {
        Name = viewModel.Name,
        Email = viewModel.Email,
        TwitterName = viewModel.TwitterName,
        Url = viewModel.Url
      });
      _orchardServices.Notifier.Add(NotifyType.Information, T("Created the person {0}", viewModel.Name));
      return RedirectToAction("Index");
    }

    /// <summary>
    /// Action for displaying the form to edit a new person.
    /// </summary>
    [HttpGet, Admin]
    public ActionResult Edit(int personId) {
      var person = _personRepository.Get(personId);
      if (person == null) {
        return new HttpNotFoundResult("Could not find the preson with id " + personId);
      }

      var viewModel = new PersonViewModel {
        Id = personId,
        Name = person.Name,
        Email = person.Email,
        Url = person.Url,
        TwitterName = person.TwitterName
      };

      // get the podcasts person is set to host
      viewModel.PodcastsHosted = _orchardServices.ContentManager.GetMany<PodcastPart>(
        person.PodcastHosted.Select(p => p.PodcastPartRecord.Id), VersionOptions.Published, QueryHints.Empty)
        ?? new List<PodcastPart>();

      // get the episodes the person has hosted (or empty collection if none)
      viewModel.EpisodesHosted = _orchardServices.ContentManager.GetMany<PodcastEpisodePart>(
          person.PodcastEpisodes.Where(p => p.IsHost == true)
                .Select(p => p.PodcastEpisodePartRecord.Id), VersionOptions.Published, QueryHints.Empty)
        ?? new List<PodcastEpisodePart>();

      // get the episodes the person has been a guest on (or empty collection if none)
      viewModel.EpisodesGuested = _orchardServices.ContentManager.GetMany<PodcastEpisodePart>(
          person.PodcastEpisodes.Where(p => p.IsHost == false)
                .Select(p => p.PodcastEpisodePartRecord.Id), VersionOptions.Published, QueryHints.Empty)
        ?? new List<PodcastEpisodePart>();

      // build the view
      return View(viewModel);
    }

    /// <summary>
    /// Action for POSTing the edit form.
    /// </summary>
    [HttpPost, ActionName("Edit"), Admin]
    public ActionResult EditPOST(PersonViewModel viewModel) {
      var person = _personRepository.Get(viewModel.Id);

      // if the model isn't valid, display it again
      if (!ModelState.IsValid) {
        // get the podcasts person is set to host
        viewModel.PodcastsHosted = _orchardServices.ContentManager.GetMany<PodcastPart>(
          person.PodcastHosted.Select(p => p.PodcastPartRecord.Id), VersionOptions.Published, QueryHints.Empty)
          ?? new List<PodcastPart>();

        // get the episodes the person has hosted (or empty collection if none)
        viewModel.EpisodesHosted = _orchardServices.ContentManager.GetMany<PodcastEpisodePart>(
            person.PodcastEpisodes.Where(p => p.IsHost == true)
                  .Select(p => p.PodcastEpisodePartRecord.Id), VersionOptions.Published, QueryHints.Empty)
          ?? new List<PodcastEpisodePart>();

        // get the episodes the person has been a guest on (or empty collection if none)
        viewModel.EpisodesGuested = _orchardServices.ContentManager.GetMany<PodcastEpisodePart>(
            person.PodcastEpisodes.Where(p => p.IsHost == false)
                  .Select(p => p.PodcastEpisodePartRecord.Id), VersionOptions.Published, QueryHints.Empty)
          ?? new List<PodcastEpisodePart>();

        return View("Edit", viewModel);
      }

      // otherwise update the person
      person.Name = viewModel.Name;
      person.Email = viewModel.Email;
      person.Url = viewModel.Url;
      person.TwitterName = viewModel.TwitterName;
      _personRepository.Update(person);

      _orchardServices.Notifier.Add(NotifyType.Information, T("Saved {0}", viewModel.Name));
      return RedirectToAction("Index");
    }

    /// <summary>
    /// Action for deleting a person.
    /// </summary>
    [HttpGet, Admin]
    public ActionResult Delete(int personId) {
      var person = _personRepository.Get(personId);
      if (person == null) {
        return new HttpNotFoundResult("Could not find the person with id = " + personId);
      }
      _personRepository.Delete(person);
      _orchardServices.Notifier.Add(NotifyType.Information, T("'{0}' has been deleted", person.Name));
      return RedirectToAction("Index");
    }

  }
}