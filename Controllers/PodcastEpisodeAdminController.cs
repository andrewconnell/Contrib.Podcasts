using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Contrib.Podcasts.Models;
using Contrib.Podcasts.Services;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Mvc;
using Orchard.Core.Contents.Settings;
using Orchard.Data;
using Orchard.Localization;
using Orchard.Settings;
using Orchard.UI.Admin;
using Orchard.UI.Notify;
using Contrib.Podcasts.Extensions;

namespace Contrib.Podcasts.Controllers {
  [ValidateInput(false), Admin]
  public class PodcastEpisodeAdminController : Controller, IUpdateModel {
    private readonly IContentManager _contentManager;
    private readonly ISiteService _siteService;
    private readonly IPodcastService _podcastService;
    private readonly IPodcastEpisodeService _podcastEpisodeService;
    private readonly IRepository<PersonRecord> _personRepository;
    private readonly IRepository<EpisodePersonRecord> _episodePersonRepository;

    public PodcastEpisodeAdminController(IOrchardServices services, IContentManager contentManager, ISiteService siteService, IPodcastService podcastService, IPodcastEpisodeService podcastEpisodeService, IRepository<PersonRecord> personRepository, IRepository<EpisodePersonRecord> episodePersonRepository) {
      Services = services;
      _contentManager = contentManager;
      _siteService = siteService;
      _podcastService = podcastService;
      _podcastEpisodeService = podcastEpisodeService;
      _personRepository = personRepository;
      _episodePersonRepository = episodePersonRepository;
    }

    public IOrchardServices Services { get; set; }
    public Localizer T { get; set; }

    /// <summary>
    /// Handles action request to create a podcast episode.
    /// </summary>
    public ActionResult Create(int podcastId) {
      var podcast = _podcastService.Get(podcastId).As<PodcastPart>();
      if (podcast == null)
        return HttpNotFound();

      if (!Services.Authorizer.Authorize(Permissions.EditPodcastEpisode, podcast, T("Not allowed to create podcast episode")))
        return new HttpUnauthorizedResult();

      var podcastEpisode = Services.ContentManager.New<PodcastEpisodePart>("PodcastEpisode");
      podcastEpisode.PodcastPart = podcast;

      // init the episode number
      var lastEpisode = _podcastEpisodeService.Get(podcast, VersionOptions.Latest)
        .OrderByDescending(e => e.EpisodeNumber).FirstOrDefault();

      // init the rating of episode = rating of podcast
      podcastEpisode.EpisodeNumber = lastEpisode == null ? 1 : lastEpisode.EpisodeNumber + 1;
      podcastEpisode.Rating = podcast.Rating;

      dynamic model = Services.ContentManager.BuildEditor(podcastEpisode);
      return View((object)model);
    }

    /// <summary>
    /// Handles POST of creating episode from create page.
    /// </summary>
    [HttpPost, ActionName("Create")]
    [FormValueRequired("submit.Save")]
    public ActionResult CreatePOST(int podcastId) {
      return CreatePOST(podcastId, false);
    }

    /// <summary>
    /// Handles POST of creating & publishing episode from create page.
    /// </summary>
    [HttpPost, ActionName("Create")]
    [FormValueRequired("submit.Publish")]
    public ActionResult CreateAndPublishPOST(int podcastId) {
      if (!Services.Authorizer.Authorize(Permissions.PublishOwnPodcastEpisode, T("Couldn't create content")))
        return new HttpUnauthorizedResult();

      return CreatePOST(podcastId, true);
    }

    private ActionResult CreatePOST(int podcastId, bool publish = false) {
      var podcast = _podcastService.Get(podcastId).As<PodcastPart>();

      if (podcast == null)
        return HttpNotFound();

      if (!Services.Authorizer.Authorize(Permissions.EditPodcastEpisode, podcast, T("Couldn't create podcast episode")))
        return new HttpUnauthorizedResult();

      var episode = Services.ContentManager.New<PodcastEpisodePart>("PodcastEpisode");
      episode.PodcastPart = podcast;

      Services.ContentManager.Create(episode, VersionOptions.Draft);
      var model = Services.ContentManager.UpdateEditor(episode, this);

      if (!ModelState.IsValid) {
        Services.TransactionManager.Cancel();
        return View((object)model);
      }

      if (publish) {
        if (!Services.Authorizer.Authorize(Permissions.PublishPodcastEpisode, podcast.ContentItem, T("Couldn't publish podcast episode")))
          return new HttpUnauthorizedResult();

        Services.ContentManager.Publish(episode.ContentItem);
      }

      Services.Notifier.Information(T("Your {0} has been created.", episode.TypeDefinition.DisplayName));
      return Redirect(Url.PodcastEpisodeEdit(episode));
    }

    /// <summary>
    /// Handles obtaining the requested episode and displaying the edit view.
    /// </summary>
    [HttpGet]
    public ActionResult Edit(int podcastId, int episodeId) {
      var podcast = _podcastService.Get(podcastId);
      if (podcast == null)
        return HttpNotFound();

      var episode = _podcastEpisodeService.Get(episodeId, VersionOptions.Latest);
      if (episode == null)
        return HttpNotFound();

      if (!Services.Authorizer.Authorize(Permissions.EditPodcastEpisode, episode, T("Couldn't edit podcast episode")))
        return new HttpUnauthorizedResult();

      dynamic model = Services.ContentManager.BuildEditor(episode);
      return View((object) model);
    }

    /// <summary>
    /// Handles POST of saving episode from edit page.
    /// </summary>
    [HttpPost, ActionName("Edit")]
    [FormValueRequired("submit.Save")]
    public ActionResult EditPOST(int podcastId, int episodeId) {
      return EditPOST(podcastId, episodeId, contentItem => {
        if (!contentItem.Has<IPublishingControlAspect>() && !contentItem.TypeDefinition.Settings.GetModel<ContentTypeSettings>().Draftable)
          Services.ContentManager.Publish(contentItem);
      });
/*
      var podcast = _podcastService.Get(podcastId);
      if (podcast == null)
        return HttpNotFound();
      
      var episode = _podcastEpisodeService.Get(episodeId, VersionOptions.Latest);
      if (episode == null)
        return HttpNotFound();

      if (!Services.Authorizer.Authorize(Permissions.EditPodcastEpisode, episode, T("Couldn't edit podcast episode")))
        return new HttpUnauthorizedResult();

      dynamic model = Services.ContentManager.UpdateEditor(episode, this);
      if (!ModelState.IsValid) {
        Services.TransactionManager.Cancel();
        return View((object)model);
      }

      _contentManager.Publish(episode.ContentItem);
      Services.Notifier.Information(T("Episode information updated"));

      return Redirect(Url.PodcastForAdmin(podcast.As<PodcastPart>()));
*/
    }

    public ActionResult EditPOST(int podcastId, int episodeId, Action<ContentItem> conditionallyPublish) {
      var podcast = _podcastService.Get(podcastId);
      if (podcast == null)
        return HttpNotFound();

      var episode = _podcastEpisodeService.Get(episodeId, VersionOptions.DraftRequired);
      if (episode == null)
        return HttpNotFound();

      if (!Services.Authorizer.Authorize(Permissions.PublishPodcastEpisode, episode, T("Couldn't publish podcast episode")))
        return new HttpUnauthorizedResult();

      dynamic model = Services.ContentManager.UpdateEditor(episode, this);
      if (!ModelState.IsValid) {
        Services.TransactionManager.Cancel();
        return View((object)model);
      }

      conditionallyPublish(episode.ContentItem);

      Services.Notifier.Information(T("Episode information updated"));

      return Redirect(Url.PodcastForAdmin(podcast.As<PodcastPart>()));
    }

    /// <summary>
    /// Handles POST of saving episode from edit page.
    /// </summary>
    [HttpPost, ActionName("Edit")]
    [FormValueRequired("submit.Publish")]
    public ActionResult EditAndPublishPOST(int podcastId, int episodeId) {
      var podcast = _podcastService.Get(podcastId);
      if (podcast == null)
        return HttpNotFound();

      var episode = _podcastEpisodeService.Get(episodeId, VersionOptions.DraftRequired);
      if (episode == null)
        return HttpNotFound();

      if (!Services.Authorizer.Authorize(Permissions.PublishPodcastEpisode, episode, T("Couldn't publish podcast episode")))
        return new HttpUnauthorizedResult();

      dynamic model = Services.ContentManager.UpdateEditor(episode, this);
      if (!ModelState.IsValid) {
        Services.TransactionManager.Cancel();
        return View((object)model);
      }

      _contentManager.Publish(episode.ContentItem);
      Services.Notifier.Information(T("Episode information updated"));

      return Redirect(Url.PodcastForAdmin(podcast.As<PodcastPart>()));
    }

    /// <summary>
    /// Handles POST of deleting episode from edit page.
    /// </summary>
    [HttpPost, ActionName("Edit")]
    [FormValueRequired("submit.Delete")]
    public ActionResult EditDeletePOST(int podcastId, int episodeId) {
      var podcast = _podcastService.Get(podcastId);
      if (podcast == null)
        return HttpNotFound();

      var episode = _podcastEpisodeService.Get(episodeId, VersionOptions.Latest);
      if (episode == null)
        return HttpNotFound();

      if (!Services.Authorizer.Authorize(Permissions.DeletePodcastEpisode, episode, T("Couldn't delete podcast episode")))
        return new HttpUnauthorizedResult();

      _podcastEpisodeService.Delete(episode.ContentItem);

      Services.Notifier.Information(T("Episode deleted"));

      return Redirect(Url.PodcastForAdmin(podcast.As<PodcastPart>()));
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