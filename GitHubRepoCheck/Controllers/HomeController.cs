/*
 * Author: Jack David Wilkinson
 * Date: 07/05/2019
 * Description: Controllers for GitHubRepoCheck
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using GitHubRepoCheck.Models;
using System.Text;

namespace GitHubRepoCheck.Controllers
{
    public class HomeController : Controller
    {
        // GET
        // Form for GitHubAPISearch
        public ActionResult GitHubAPI()
        {
            return View();
        }

        // GET
        // Queries GitHub API for input username and returns the
        // avatar picture, location, and name for the user.
        // Also returns links to the top 5 repos (those which have
        // the most stars).
        // TO DO: - Tidy boilerplate code/magic variables.
        //        - Look into separating api requests/parsing
        //          through services
        public ActionResult GitHubAPISearch(String Username)
        {
            GitHubAPISearchResults resultsModel = new GitHubAPISearchResults();

            try
            {
                // Longer loading times for *large* repo lists; i.e. in the magnitude of thousands,
                // meaning more api calls, as github api response pages are limited to 100 items.
                string url = "https://api.github.com/users/" + Username;

                // Create an authenticated (if required) request for the URL. 
                // Insert your token in place of ***** for testing and uncomment
                // the last line in this block; 
                // see https://help.github.com/en/articles/creating-a-personal-access-token-for-the-command-line
                HttpWebRequest requestOriginal = (HttpWebRequest)WebRequest.Create(url);
                requestOriginal.UserAgent = ("JDW_WebDevTests_2019");
                requestOriginal.Accept = "application/vnd.github.v3+json";
                //var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes("username:*****"));
                //requestOriginal.Headers.Add("Authorization", $"Basic {base64authorization}");

                requestOriginal.Method = "GET";
                requestOriginal.Timeout = 60000;

                // Get the stream containing content returned by the server. 
                // The using blocks ensure the stream/response are automatically closed.
                string jsonResponseOriginal = string.Empty;
                using (WebResponse response = requestOriginal.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        jsonResponseOriginal = reader.ReadToEnd();
                    }
                    Debug.WriteLine("Status: " + ((HttpWebResponse)response).StatusDescription);
                }

                // Parse the respone and assign to the model.
                JObject jsonObjOriginal = JObject.Parse(jsonResponseOriginal);
                resultsModel.username = jsonObjOriginal.GetValue("name").ToString();
                resultsModel.location = jsonObjOriginal.GetValue("location").ToString();
                resultsModel.avatarURL = jsonObjOriginal.GetValue("avatar_url").ToString();
                resultsModel.repoURL = jsonObjOriginal.GetValue("repos_url").ToString();

                // Call the repo API until all of the information is retrieved/failure.
                bool moreData = true;
                int counter = 0;
                Dictionary<string, int> URLAndStars = new Dictionary<string, int>();
                while (moreData)
                {
                    // Create an authenticated (if required) request for the URL. 
                    // Insert your token in place of ***** for testing and uncomment
                    // the last line in this block; 
                    // see https://help.github.com/en/articles/creating-a-personal-access-token-for-the-command-line
                    HttpWebRequest requestMore = (HttpWebRequest)WebRequest.Create(resultsModel.repoURL);
                    requestMore.UserAgent = ("JDW_WebDevTests_2019");
                    requestMore.Accept = "application/vnd.github.v3+json";
                    //base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes("username:*****"));
                    //requestMore.Headers.Add("Authorization", $"Basic {base64authorization}");

                    requestMore.Method = "GET";
                    requestMore.Timeout = 60000;

                    // Get the stream containing content returned by the server. 
                    // The using blocks ensure the stream/response are automatically closed.
                    string jsonResponseMore = string.Empty;
                    string link = string.Empty;
                    using (WebResponse response = requestMore.GetResponse())
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            jsonResponseMore = reader.ReadToEnd();
                            link = response.Headers["Link"];
                        }

                        Debug.WriteLine("API call: " + (counter + 1));
                        Debug.WriteLine("Status: " + ((HttpWebResponse)response).StatusDescription);
                    }

                    // Parse the 'link' respone to determine if more data needs to be loaded
                    // (see https://developer.github.com/v3/#pagination)
                    string FirstLink = String.Empty;
                    string PreviousLink = String.Empty;
                    string NextLink = String.Empty;
                    string LastLink = String.Empty;

                    if (link != null)
                    {
                        string[] values = link.Split(',');
                        Debug.WriteLine(link);

                        foreach (string element in values)
                        {
                            var relMatch = Regex.Match(element, "(?<=rel=\").+?(?=\")", RegexOptions.IgnoreCase);
                            var linkMatch = Regex.Match(element, "(?<=<).+?(?=>)", RegexOptions.IgnoreCase);

                            if (relMatch.Success && linkMatch.Success)
                            {
                                string relVal = relMatch.Value.ToUpper();
                                string linkVal = linkMatch.Value;

                                switch (relVal)
                                {
                                    case "FIRST":
                                        FirstLink = linkVal;
                                        break;
                                    case "PREV":
                                        PreviousLink = linkVal;
                                        break;
                                    case "NEXT":
                                        NextLink = linkVal;
                                        break;
                                    case "LAST":
                                        LastLink = linkVal;
                                        break;
                                }
                            }
                        }
                    }

                    // Parse the new response and sort the resulting JArray.
                    JArray jsonArr = JArray.Parse(jsonResponseMore);
                    JArray sorted = new JArray(jsonArr.OrderByDescending(jar => (int)jar["stargazers_count"]));

                    int topCounter = 0;
                    for (int j = 0; j < sorted.Count(); j++)
                    {
                        Debug.WriteLine(sorted[j]["html_url"]);
                        Debug.WriteLine(sorted[j]["stargazers_count"]);
                        URLAndStars.Add((string)sorted[j]["html_url"], (int)sorted[j]["stargazers_count"]);
                        topCounter++;
                        if (topCounter >= 5)
                            break;
                    }

                    // No more data to be loaded. Assign the repo url/stars model information
                    if (NextLink == String.Empty)
                    {
                        moreData = false;

                        Debug.WriteLine("\n");
                        Debug.WriteLine("Dictionary unordered: ");

                        foreach (KeyValuePair<string, int> kvp in URLAndStars)
                        {
                            Debug.WriteLine(string.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value));
                        }

                        Debug.WriteLine("\n");
                        Debug.WriteLine("Dictionary ordered by descending stargazer count: ");

                        foreach (KeyValuePair<string, int> kvp in URLAndStars.OrderByDescending(key => key.Value))
                        {
                            Debug.WriteLine(string.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value));
                        }

                        Debug.WriteLine("\n");
                        Debug.WriteLine("Top 5: ");

                        topCounter = 0;
                        foreach (KeyValuePair<string, int> kvp in URLAndStars.OrderByDescending(key => key.Value))
                        {
                            Debug.WriteLine(string.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value));
                            resultsModel.stars.Add(kvp.Key, kvp.Value);
                            topCounter++;
                            if (topCounter >= 5)
                                break;
                        }
                    }
                    Debug.WriteLine("\n");
                    resultsModel.repoURL = NextLink;
                    counter++;
                }
            }

            // Currently any and all program errors will result in this 'blanket' default.
            // TO D0: Separate exceptions into multiple types/fine-tune resulting view.
            catch (Exception e)
            {
                resultsModel.username = resultsModel.location
                = resultsModel.avatarURL = resultsModel.repoURL
                = "FAILURE - Cannot process or parse data.";
                Debug.WriteLine(e);
                return View(resultsModel);
            }

            return View(resultsModel);
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}