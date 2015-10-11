using System;
using System.Reflection.Emit;
using Contrib.Podcasts.Models;
using Orchard.ContentManagement.Records;
using Orchard.Core.Contents.Extensions;
using Orchard.Data;
using Orchard.Data.Migration;
using Orchard.ContentManagement.MetaData;

namespace Contrib.Podcasts {
  public class Migrations : DataMigrationImpl {
    private readonly IRepository<PersonRecord> _personRepository;
    private readonly IRepository<PodcastPartRecord> _podcastRepository;
    private readonly IRepository<PodcastEpisodePartRecord> _podcastEpisodePart;

    public Migrations(IRepository<PersonRecord> personRepository, IRepository<PodcastPartRecord> podcastRepository, IRepository<PodcastEpisodePartRecord> podcastEpisodePart) {
      _personRepository = personRepository;
      _podcastRepository = podcastRepository;
      _podcastEpisodePart = podcastEpisodePart;
    }

    /// <summary>
    /// Create the schema in the system.
    /// </summary>
    public int Create() {

      CreatePodcastEntity();

      CreateEpisodeEntity();

      CreateRecentEpisodesWidget();

      CreatePersonStructures();

#if DEBUG // populate with sample data
      SampleDataCreatePeople();
#endif

      return 1;
    }

    public int UpdateFrom1() {
      UpdateTableTo2();

      return 2;
    }

    private void CreateRecentEpisodesWidget() {
      SchemaBuilder.CreateTable("RecentPodcastEpisodesPartRecord", table => table
        .ContentPartRecord()
        .Column<int>("PodcastId")
        .Column<int>("Count")
        );

      ContentDefinitionManager.AlterTypeDefinition("RecentPodcastEpisodes", builder => builder
        .WithPart("RecentPodcastEpisodesPart")
        .WithPart("CommonPart")
        .WithPart("WidgetPart")
        .WithSetting("Stereotype","Widget")
        );
    }

    /// <summary>
    /// Creates tables for people to support storing show hosts & guests.
    /// </summary>
    public void CreatePersonStructures() {
      // create table for people
      SchemaBuilder.CreateTable("PersonRecord", table => table
        .Column<int>("Id", column => column.PrimaryKey().Identity())
        .Column<string>("Name")
        .Column<string>("Email")
        .Column<string>("Url")
        .Column<string>("TwitterName")
        );
      // create table for joining people as hosts to podcast
      SchemaBuilder.CreateTable("PodcastHostRecord", table => table
        .Column<int>("Id", column => column.PrimaryKey().Identity())
        .Column<int>("PodcastPartRecord_Id")
        .Column<int>("PersonRecord_Id")
        );
      // create table for joining people as guests to episodes
      SchemaBuilder.CreateTable("EpisodePersonRecord", table => table
        .Column<int>("Id", column => column.PrimaryKey().Identity())
        .Column<int>("PodcastEpisodePartRecord_Id")
        .Column<int>("PersonRecord_Id")
        .Column<bool>("IsHost")
        );
    }

    /// <summary>
    /// Create the entity to store podcasts.
    /// </summary>
    private void CreatePodcastEntity() {
      SchemaBuilder.CreateTable("PodcastPartRecord", table => table
        .ContentPartRecord()
        .Column<string>("Description", c => c.Unlimited())
        .Column<string>("Rating")
        .Column<string>("CreativeCommonsLicense")
        .Column<bool>("IncludeTranscriptInFeed")
        );

      ContentDefinitionManager.AlterTypeDefinition("Podcast", builder => builder
        .WithPart("PodcastPart")
        .WithPart("CommonPart")
        .WithPart("TitlePart")
        .WithPart("AutoroutePart", partBuilder => partBuilder
          .WithSetting("AutorouteSettings.AllowCustomPattern", "true")
          .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "false")
          .WithSetting("AutorouteSettings.PatternDefinitions", "[{Name:'Podcast', Pattern: '{Content.Slug}', Description: 'Podcast'}]")
          .WithSetting("AutorouteSettings.DefaultPatternIndex", "0")
          )
        .WithPart("MenuPart")
        .WithPart("AdminMenuPart", amp => amp.WithSetting("AdminMenuPartTypeSettings.DefaultPosition", "3"))
        );

      // create podcast part with an image field
      ContentDefinitionManager.AlterPartDefinition("PodcastPart", builder => builder
        .WithField("Image", fieldBuilder => fieldBuilder.OfType("MediaPickerField"))
        );
    }

    /// <summary>
    /// Create entity to store episodes.
    /// </summary>
    private void CreateEpisodeEntity() {
      // create table to store episodes
      SchemaBuilder.CreateTable("PodcastEpisodePartRecord", table => table
        .ContentPartRecord()
        .Column<int>("PodcastId", column => column.NotNull())
        .Column<int>("EpisodeNumber", column => column.Unique())
        .Column<string>("EnclosureUrl")
        .Column<string>("Duration", column => column.WithLength(5))
        .Column<decimal>("EnclosureFilesize", column => column.WithScale(2))
        .Column<string>("Rating", column => column.WithLength(8))
        );

      // create episode content type
      ContentDefinitionManager.AlterTypeDefinition("PodcastEpisode", builder => builder
        .WithPart("PodcastEpisodePart")
        .WithPart("CommonPart", p => p.WithSetting("DateEditorSettings.ShowDateEditor", "true"))
        .WithPart("PublishLaterPart")
        .WithPart("TitlePart")
        .WithPart("AutoroutePart", partBuilder => partBuilder
          .WithSetting("AutorouteSettings.AllowCustomPattern", "true")
          .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "false")
          .WithSetting("AutorouteSettings.PatternDefinitions", "[{Name:'Episode Title', Pattern: '{Content.Container.Path}/Episodes/{Content.Slug}', Description: 'my-podcast/Episodes/Episode-Title'}," +
                                                                "{Name:'Show Title', Pattern: '{Content.Container.Path}/Shows/{Content.Slug}', Description: 'my-podcast/Shows/Show-Title'}]")
          .WithSetting("AutorouteSettings.DefaultPatternIndex", "0")
          )
        .Draftable()
        );

      // create fields fields
      ContentDefinitionManager.AlterPartDefinition("PodcastEpisodePart", builder => builder
        .WithField("RecordedDate", fieldBuilder => fieldBuilder
          .OfType("DateTimeField")
          .WithDisplayName("Recorded Date")
        )
        .WithField("ShowNotes", fieldBuilder => fieldBuilder
          .OfType("TextField")
          .WithDisplayName("Show Notes")
          .WithSetting("TextFieldSettings.Flavor", "html")
        )
        .WithField("ShowTranscript", fieldBuilder => fieldBuilder
          .OfType("TextField")
          .WithDisplayName("Show Transcript")
          .WithSetting("TextFieldSettings.Flavor", "html")
        )
      );
    }

    /// <summary>
    /// Updates the DB table to v1.1
    /// </summary>
    private void UpdateTableTo2() {
      // add columns for RSS feed
      SchemaBuilder.AlterTable("PodcastPartRecord", t => t
        .AddColumn<string>("ContactEmail")
        );
      SchemaBuilder.AlterTable("PodcastPartRecord", t => t
        .AddColumn<string>("Keywords")
        );
      SchemaBuilder.AlterTable("PodcastPartRecord", t => t
        .AddColumn<string>("Subtitle")
        );
      SchemaBuilder.AlterTable("PodcastPartRecord", t => t
        .AddColumn<string>("Summary")
        );
      SchemaBuilder.AlterTable("PodcastPartRecord", t => t
        .AddColumn<string>("CultureCode")
        );
      SchemaBuilder.AlterTable("PodcastPartRecord", t => t
        .AddColumn<string>("LogoImageUrl")
        );
      SchemaBuilder.AlterTable("PodcastPartRecord", t => t
        .AddColumn<string>("UpdateFrequency")
        );
      SchemaBuilder.AlterTable("PodcastPartRecord", t => t
        .AddColumn<int>("UpdatePeriod")
        );
      SchemaBuilder.AlterTable("PodcastPartRecord", t => t
        .AddColumn<string>("Categories")
        );
    }

#if DEBUG
    private void SampleDataCreatePeople() {
      _personRepository.Create(new PersonRecord { Name = "Ken Sanchez", Email = "ken.sanchez@swampland.local", Url = "http://www.foo.com/blogs/ken.sanchez", TwitterName = "kensanchez" });
      _personRepository.Create(new PersonRecord { Name = "Janice Galvin", Email = "janice.galvin@swampland.local", Url = "http://www.foo.com/blogs/janice.galvin", TwitterName = "janicegalvin" });
      _personRepository.Create(new PersonRecord { Name = "Rob Walters", Email = "rob.walters@swampland.local", Url = "http://www.foo.com/blogs/rob.walters", TwitterName = "robwalters" });
      _personRepository.Create(new PersonRecord { Name = "Brian Cox", Email = "brian.cox@swampland.local", Url = "http://www.foo.com/blogs/brian.cox", TwitterName = "briancox" });
    }
#endif
  }
}