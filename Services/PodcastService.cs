﻿using System.Collections.Generic;
using System.Linq;
using Contrib.Podcasts.Models;
using Contrib.Podcasts.ViewModels;
using Orchard.ContentManagement;
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

      // get list of all hosts currently in the DB for this podcast
      var oldHosts = _podcastHostRespository.Fetch(host => host.PodcastPartRecord.Id == part.Id).Select(r => r.PersonRecord.Id).ToList();
      // remove all the hosts not in the new list from the DB
      foreach (var oldHostId in oldHosts.Except(viewModel.Hosts)) {
        _podcastHostRespository.Delete(_podcastHostRespository.Get(record => record.PersonRecord.Id == oldHostId));
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