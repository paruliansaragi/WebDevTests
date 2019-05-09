using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GitHubRepoCheck.Models
{
    public class GitHubAPISearchResults
    {
        public string username { get; set; }
        public string location { get; set; }
        public string avatarURL { get; set; }
        public string repoURL { get; set; }

        public Dictionary<string, int> stars = new Dictionary<string, int>();
    }
}