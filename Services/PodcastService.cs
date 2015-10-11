using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Contrib.Podcasts.Models;
using Contrib.Podcasts.ViewModels;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Title.Models;
using Orchard.Data;

namespace Contrib.Podcasts.Services {
  public class PodcastService : IPodcastService {
    private readonly IContentManager _contentManager;
    private readonly IRepository<PersonRecord> _personRepository;
    private readonly IRepository<PodcastHostRecord> _podcastHostRespository;

    public PodcastService(IContentManager contentManager, IRepository<PersonRecord> personRepository, IRepository<PodcastHostRecord> podcastHostRespository) {
      _contentManager = contentManager;
      _personRepository = personRepository;
      _podcastHostRespository = podcastHostRespository;
    }

    /// <summary>
    /// Get podcasts or a specific podcast
    /// </summary>
    public IEnumerable<PodcastPart> Get() {
      return _contentManager.Query<PodcastPart, PodcastPartRecord>(VersionOptions.Published)
                            .Join<TitlePartRecord>()
                            .OrderBy(p => p.Title)
                            .List();
    }

    /// <summary>
    /// Gets a specific podcast part based on the path passed in.
    /// </summary>
    public PodcastPart Get(string path) {
      return _contentManager.Query<PodcastPart>().List().FirstOrDefault(r => r.As<IAliasAspect>().Path == path);
    }

    /// <summary>
    /// Gets specific podcast content item by the podcast id.
    /// </summary>
    public ContentItem Get(int podcastId) {
      return _contentManager.Get(podcastId, VersionOptions.Latest);
    }

    /// <summary>
    /// Update a podcast using the specified view model part. This is needed as some things in the add/edit UI aren't 
    /// handled automatically by Orchard (like the hosts selection).
    /// </summary>
    public void Update(PodcastViewModel viewModel, PodcastPart part) {
      part.Rating = viewModel.Rating;
      part.CreativeCommonsLicense = viewModel.License;
      part.IncludeTranscriptInFeed = viewModel.IncludeEpisodeTranscriptInFeed;
      part.Description = viewModel.Description;
      part.ContactEmail = viewModel.ContactEmail;
      part.Keywords = viewModel.Keywords;
      part.Subtitle = viewModel.Subtitle;
      part.Summary = viewModel.Summary;
      part.CultureCode = viewModel.CultureCode;
      part.LogoImageUrl = viewModel.LogoImageUrl;
      part.UpdateFrequency = viewModel.UpdateFrequency;
      part.UpdatePeriod = viewModel.UpdatePeriod;
      part.PodcastCategories = viewModel.PodcastCategories;

      // get list of all hosts currently in the DB for this podcast
      var oldHosts = _podcastHostRespository.Fetch(host => host.PodcastPartRecord.Id == part.Id).Select(r => r.PersonRecord.Id).ToList();

      // remove all the hosts not in the new list from the DB
      IEnumerable<int> hostsToRemove = viewModel.Hosts == null
        ? (IEnumerable<int>)oldHosts
        : (IEnumerable<int>)oldHosts.Except(viewModel.Hosts);
      foreach (var oldHostId in oldHosts.Except(viewModel.Hosts)) {
        var hostToRemove = _podcastHostRespository.Get(record =>
                                                        record.PersonRecord.Id == oldHostId &&
                                                        record.PodcastPartRecord.Id == part.Id);
        _podcastHostRespository.Delete(hostToRemove);
      }

      // add all new hosts not in the DB that are in the new list
      foreach (var newHostId in viewModel.Hosts.Except(oldHosts)) {
        var host = _personRepository.Get(newHostId);
        _podcastHostRespository.Create(new PodcastHostRecord {
          PersonRecord = host,
          PodcastPartRecord = part.Record
        });
      }
    }

    /// <summary>
    /// Removes specified podcast.
    /// </summary>
    public void Delete(ContentItem podcastPart) {
      // TODO: removes hosts?
      // TODO: removes episodes?
      _contentManager.Remove(podcastPart);
    }
  }
}