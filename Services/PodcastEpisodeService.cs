﻿using System;
using System.Collections.Generic;
using System.Linq;
using Contrib.Podcasts.Models;
using Contrib.Podcasts.ViewModels;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Tasks.Scheduling;
using Orchard.Data;

namespace Contrib.Podcasts.Services {
  public class PodcastEpisodeService : IPodcastEpisodeService {
    private readonly IContentManager _contentManager;
    private readonly IPublishingTaskManager _publishingTaskManager;
    private readonly IRepository<PersonRecord> _personRepository;
    private readonly IRepository<EpisodePersonRecord> _episodePersonRepository;

    public PodcastEpisodeService(IContentManager contentManager, IPublishingTaskManager publishingTaskManager, IRepository<PersonRecord> personRepository, IRepository<EpisodePersonRecord> episodePersonRepository) {
      _contentManager = contentManager;
      _publishingTaskManager = publishingTaskManager;
      _personRepository = personRepository;
      _episodePersonRepository = episodePersonRepository;
    }

    /// <summary>
    /// Get a specific published podcast episode.
    /// </summary>
    public PodcastEpisodePart Get(int id) {
      return Get(id, VersionOptions.Published);
    }

    /// <summary>
    /// Get a specific podcast episode with a specific version.
    /// </summary>
    public PodcastEpisodePart Get(int id, VersionOptions versionOptions) {
      return _contentManager.Get<PodcastEpisodePart>(id, versionOptions);
    }

    /// <summary>
    /// Get list of all episodes for a podcast.
    /// </summary>
    public IEnumerable<PodcastEpisodePart> Get(PodcastPart podcastPart) {
      return Get(podcastPart, VersionOptions.Published);
    }

    /// <summary>
    /// Get list of all episodes for a podcast.
    /// </summary>
    public IEnumerable<PodcastEpisodePart> Get(PodcastPart podcastPart, VersionOptions versionOptions) {
      return GetPodcastQuery(podcastPart, versionOptions)
        .List()
        .Select(ci => ci.As<PodcastEpisodePart>());
    }

    /// <summary>
    /// Get list of all episodes for a podcast.
    /// </summary>
    public IEnumerable<PodcastEpisodePart> Get(PodcastPart podcastPart, int skip, int count) {
      return Get(podcastPart, skip, count, VersionOptions.Published);
    }

    /// <summary>
    /// Get list of all episodes for a podcast.
    /// </summary>
    public IEnumerable<PodcastEpisodePart> Get(PodcastPart podcastPart, int skip, int count, VersionOptions versionOptions) {
      return GetPodcastQuery(podcastPart, versionOptions)
        .Slice(skip, count)
        .ToList()
        .Select(ci => ci.As<PodcastEpisodePart>());
    }

    /// <summary>
    /// Get count of all episodes for a podcast.
    /// </summary>
    public int EpisodeCount(PodcastPart podcastPart) {
      return EpisodeCount(podcastPart, VersionOptions.Published);
    }

    /// <summary>
    /// Get count of all episodes for a podcast.
    /// </summary>
    public int EpisodeCount(PodcastPart podcastPart, VersionOptions versionOptions) {
      return GetPodcastQuery(podcastPart, versionOptions).Count();
    }

    private IContentQuery<ContentItem, CommonPartRecord> GetPodcastQuery(ContentPart<PodcastPartRecord> podcast, VersionOptions versionOptions) {
      return _contentManager.Query(versionOptions, "PodcastEpisode")
        .Join<CommonPartRecord>()
        .Where(cpr => cpr.Container == podcast.Record.ContentItemRecord)
        .OrderByDescending(cpr => cpr.CreatedUtc)
        .WithQueryHintsFor("PodcastEpisode");
    }

    /// <summary>
    /// Update an episode using the specified view model part. This is needed as some things in the add/edit UI aren't
    /// handled automatically by Orchard (like hosts & guests selection).
    /// </summary>
    public void Update(PodcastEpisodeViewModel viewModel, PodcastEpisodePart part) {
      part.PodcastId = part.PodcastPart.Id;
      part.EpisodeNumber = viewModel.EpisodeNumber;
      part.EnclosureUrl = viewModel.EnclosureUrl;
      part.EnclosureFilesize = viewModel.EnclosureFileSize;
      part.Duration = viewModel.Duration;
      part.Rating = viewModel.Rating;

      #region handle hosts
      // get list of all hosts currently in DB for this episode
      var oldHosts = _episodePersonRepository.Fetch(p => p.PodcastEpisodePartRecord.Id == part.Id && p.IsHost).Select(r => r.PersonRecord.Id).ToList();
      // remove all hosts not in the new list from the DB
      foreach (var oldHostId in oldHosts.Except(viewModel.Hosts)) {
        _episodePersonRepository.Delete(_episodePersonRepository.Get(record => record.PersonRecord.Id == oldHostId));
      }
      // add all new hosts not in the DB that are in the new list
      foreach (var newHostId in viewModel.Hosts.Except(oldHosts)) {
        var host = _personRepository.Get(newHostId);
        _episodePersonRepository.Create(new EpisodePersonRecord {
          PersonRecord = host,
          PodcastEpisodePartRecord = part.Record,
          IsHost = true
        });
      }
      #endregion

      #region handle guests
      // get list of all guests currently in DB for this episode
      var oldGuests = _episodePersonRepository.Fetch(p => p.PodcastEpisodePartRecord.Id == part.Id && !p.IsHost).Select(r => r.PersonRecord.Id).ToList();
      // remove all guests not in the new list from the DB
      foreach (var oldGuestId in oldGuests.Except(viewModel.Guests)) {
        _episodePersonRepository.Delete(_episodePersonRepository.Get(record => record.PersonRecord.Id == oldGuestId));
      }
      // add all new guests not in the DB that are in the new list
      foreach (var newGuestId in viewModel.Guests.Except(oldGuests)) {
        var guest = _personRepository.Get(newGuestId);
        _episodePersonRepository.Create(new EpisodePersonRecord {
          PersonRecord = guest,
          PodcastEpisodePartRecord = part.Record,
          IsHost = false
        });
      }
      #endregion
    }

    public void Delete(ContentItem episodePart) {
      // todo: remove episodes?
      _contentManager.Remove(episodePart);
    }
  }
}