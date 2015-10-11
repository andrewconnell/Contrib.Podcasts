using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Contrib.Podcasts.Models {

  public class PodcastCategories {
    [JsonProperty(PropertyName = "categories")]
    public Category[] Categories { get; set; }
  }

  public class Category {
    [JsonProperty(PropertyName = "title")]
    public string Title { get; set; }
    [JsonProperty(PropertyName = "subcategory")]
    public string Subcategory { get; set; }
  }

}