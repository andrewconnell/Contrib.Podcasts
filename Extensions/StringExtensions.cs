using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Contrib.Podcasts.Extensions {
  public static class StringExtensions {
    public static string SplitCamelCase (this string str) {
      return Regex.Replace(
        Regex.Replace(str,
                      @"(\P{Ll})(\P{Ll}\p{Ll})",
                      "$1 $2"),
        @"(\p{Ll})(\P{Ll})",
        "$1 $2");
    }
  }
}