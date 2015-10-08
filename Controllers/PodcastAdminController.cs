using System.Linq;
using System.Web.Mvc;
using Contrib.Podcasts.Extensions;
using Contrib.Podcasts.Models;
using Contrib.Podcasts.Services;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Mvc;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Settings;
using Orchard.UI.Admin;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;

namespace Contrib.Podcasts.Controllers {
  [ValidateInput(true), Admin]
  public class PodcastAdminController : Controller, IUpdateModel {
    private readonly IContentManager _contentManager;
    private readonly ISiteService _siteService;
    private readonly IPodcastService _podcastService;
    private readonly IPodcastEpisodeService _podcastEpisodeService;

    dynamic Shape { get; set; }
    public Localizer T { get; set; }
    public IOrchardServices Services { get; set; }

    public PodcastAdminController(IOrchardServices services, ISiteService siteService, IContentManager contentManager, IPodcastService podcastService, IPodcastEpisodeService podcastEpisodeService, IShapeFactory shapeFactory) {
      Services = services;
      Shape = shapeFactory;
      _siteService = siteService;
      _contentManager = contentManager;
      _podcastService = podcastService;
      _podcastEpisodeService = podcastEpisodeService;
    }

    /// <summary>
    /// Handles request for getting list of all podcasts.
    /// </summary>
    public ActionResult List() {
      var list = Services.New.List();
      list.AddRange(_podcastService.Get()
                                   .Select(p => {
                                     var podcast = Services.ContentManager.BuildDisplay(p, "SummaryAdmin");
                                     // TODO: add total episodes count (see same function in blogs module)
                                     return podcast;
                                   }));

      // create viewmodel
      dynamic viewModel = Services.New.ViewModel().ContentItems(list);
      return View((object)viewModel);
    }

    /// <summary>
    /// Handles showing the podcast admin display page: contains basics about podcast + all episode list + link to podcast edit page.
    /// </summary>
    public ActionResult Item(int podcastId, PagerParameters pagerParameters) {
      Pager pager = new Pager(_siteService.GetSiteSettings(), pagerParameters);
      PodcastPart podcastPart = _podcastService.Get(podcastId).As<PodcastPart>();
      if (podcastPart == null)
        return HttpNotFound();

      // get all episodes & episode shapes for the podcast
      var episodes = _podcastEpisodeService.Get(podcastPart, pager.GetStartIndex(), pager.PageSize, VersionOptions.Latest).ToArray();
      var episodesShapes = episodes.Select(pe => _contentManager.BuildDisplay(pe, "SummaryAdmin")).ToArray();

      // create dynamic podcast object
      dynamic podcast = Services.ContentManager.BuildDisplay(podcastPart, "DetailAdmin");

      // add all episode shapes to the podcast
      var list = Shape.List();
      list.AddRange(episodesShapes);
      podcast.Content.Add(Shape.Parts_Podcasts_Episode_ListAdmin(ContentItems: list), "5");

      // add pager to the 
      int totalEpisodes = _podcastEpisodeService.EpisodeCount(podcastPart, VersionOptions.Latest);
      podcast.Content.Add(Shape.Pager(pager).TotalItemCount(totalEpisodes), "Content:after");

      return View((object)podcast);
    }

    /// <summary>
    /// Handles obtaining the requested podcast and displaying the edit view.
    /// </summary>
    [HttpGet]
    public ActionResult Edit(int podcastId) {
      var podcast = _podcastService.Get(podcastId);

      if (podcast == null)
        return HttpNotFound();

      if (!Services.Authorizer.Authorize(Permissions.ManagePodcasts, podcast, T("Not allowed to edit podcast")))
        return new HttpUnauthorizedResult();

      dynamic model = Services.ContentManager.BuildEditor(podcast);
      return View((object)model);
    }

    /// <summary>
    /// Handles POST of saving podcast from edit page.
    /// </summary>
    [HttpPost, ActionName("Edit")]
    [FormValueRequired("submit.Save")]
    public ActionResult EditPOST(int podcastId) {
      var podcast = _podcastService.Get(podcastId);
      if (podcast == null)
        return HttpNotFound();

      if (!Services.Authorizer.Authorize(Permissions.ManagePodcasts, podcast, T("Couldn't edit podcast")))
        return new HttpUnauthorizedResult();

      dynamic model = Services.ContentManager.UpdateEditor(podcast, this);
      if (!ModelState.IsValid) {
        Services.TransactionManager.Cancel();
        return View((object)model);
      }

      _contentManager.Publish(podcast);
      Services.Notifier.Information(T("Podcast information updated"));

      PodcastPart podcastPart = _podcastService.Get(podcastId).As<PodcastPart>();
      return Redirect(Url.PodcastForAdmin(podcastPart));
    }

    /// <summary>
    /// Handles POST of deleting podcast from edit page.
    /// </summary>
    [HttpPost, ActionName("Edit")]
    [FormValueRequired("submit.Delete")]
    public ActionResult EditDeletePOST(int podcastId) {
      if (!Services.Authorizer.Authorize(Permissions.ManagePodcasts, T("Couldn't delete podcast")))
        return new HttpUnauthorizedResult();

      var podcast = _podcastService.Get(podcastId);
      if (podcast == null)
        return HttpNotFound();

      _podcastService.Delete(podcast);

      Services.Notifier.Information(T("Podcast deleted"));

      return Redirect(Url.PodcastsForAdmin());
    }


    #region IUpdateModel members
    bool IUpdateModel.TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) {
      return TryUpdateModel(model, prefix, includeProperties, excludeProperties);
    }

    void IUpdateModel.AddModelError(string key, LocalizedString errorMessage) {
      ModelState.AddModelError(key, errorMessage.ToString());
    }
    #endregion
  }
}