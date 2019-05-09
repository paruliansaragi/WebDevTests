# WebDevTests
A collection of test projects completed during my own time outside of work; for my continued interest in web dev!

# GitHubRepoCheck
- Click on the 'GitHubAPI' tab on the navigation bar.
- Search for a GitHub user.
- Valid usernames return the user's name, location,
  and avatar profile pic. The 5 most popular repos
  (no. of stars) are also listed in descending order.
- Some requests may take longer to load if the public
  repository lists are large, such as those for 'Google'.
  The application loads JSON batches of 100 repos per 
  page (the max supported by GitHub). See the code for
  my implementation details (GitHubAPISearch action)
  regarding pagination.
  
JDW