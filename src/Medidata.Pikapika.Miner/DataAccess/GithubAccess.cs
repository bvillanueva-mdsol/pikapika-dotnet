using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Medidata.Pikapika.Miner.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Medidata.Pikapika.Miner.DataAccess
{
    public class GithubAccess
    {
        private readonly Uri _githubBaseUri;

        private readonly string _authorization;

        private HttpClient _client;

        private int RetryMaxLimit => 3;

        public GithubAccess(Uri githubBaseUri,
            string authorizationUsername, string authorizationToken)
        {
            _githubBaseUri = githubBaseUri;
            _authorization =  $"{authorizationUsername}:{authorizationToken}";

            SetHttpClient();
        }

        private void SetHttpClient()
        {
            _client = new HttpClient() { BaseAddress = _githubBaseUri };
            var byteArray = Encoding.ASCII.GetBytes(_authorization);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.BasicAuthScheme, Convert.ToBase64String(byteArray));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.JsonContentType));
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue(Constants.UserAgent)));
        }

        public async Task<IEnumerable<Models.SearchCodeApi.ResultItem>> SearchDotnetFiles(
            string query, string extension, string repo, string path = null)
        {
            var searchItems = new List<Models.SearchCodeApi.ResultItem>();

            var extensionPara = !string.IsNullOrWhiteSpace(extension) ? $"+extension:{extension}" : "";
            var repoPara = !string.IsNullOrWhiteSpace(repo) ? $"+repo:{repo}" : "";
            var pathPara = !string.IsNullOrWhiteSpace(path) ? $"+path:{path}" : "";
            var requestUri = $"search/code?q={query}+in:path{extensionPara}{repoPara}{pathPara}&per_page=50";

            while (!string.IsNullOrWhiteSpace(requestUri))
            {
                var response = await SendWithBasicAuthAsync(HttpMethod.Get, requestUri);

                var stringResult = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Models.SearchCodeApi.Result>(stringResult);
                searchItems.AddRange(result.Items);
                Console.WriteLine($"{repo} Search Code Items Fetched, Total-Expected:{result.TotalCount}, Count: {searchItems.Count()}");

                requestUri = null;
                // Check for next page
                if (response.Headers.TryGetValues("Link", out IEnumerable<string> headerLinks) &&
                    headerLinks.Any(headerLink => headerLink.Contains("rel=\"next\"")))
                {
                    var rawNextLink = headerLinks
                        .Where(headerLink => headerLink.Contains("rel=\"next\"")).First().Split(',')
                        .Where(rawLink => rawLink.Contains("rel=\"next\"")).First();
                    var nextLinkStartIndicatorIndex = rawNextLink.IndexOf('<');
                    var nextLinkEndIndicatorIndex = rawNextLink.IndexOf('>');
                    requestUri = rawNextLink.Substring(
                        nextLinkStartIndicatorIndex + 1,
                        nextLinkEndIndicatorIndex - (nextLinkStartIndicatorIndex + 1));
                }
            }

            return searchItems;
        }

        public async Task<string> GetFileContent(string requestUri)
        {
            var response = await SendWithBasicAuthAsync(HttpMethod.Get, requestUri);

            var stringResult = await response.Content.ReadAsStringAsync();
            var jObject = JObject.Parse(stringResult);
            var data = Convert.FromBase64String(jObject["content"].ToString());
            string decodedString = Encoding.UTF8.GetString(data);
            string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (decodedString.StartsWith(_byteOrderMarkUtf8))
            {
                decodedString = decodedString.Remove(0, _byteOrderMarkUtf8.Length);
            }

            return decodedString;
        }

        private async Task<HttpResponseMessage> SendWithBasicAuthAsync(HttpMethod httpMethod, string requestUri)
        {
            var retryCount = 0;
            while (retryCount < RetryMaxLimit)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                var response = await _client.SendAsync(request).ConfigureAwait(false);

                // Forbidden(403), check retry after value and retry later
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden &&
                    response.Headers.TryGetValues("Retry-After", out IEnumerable<string> retryAfters) &&
                    retryAfters.Any() &&
                    int.TryParse(retryAfters.First(), out int retryAfter))
                {
                    Console.WriteLine($"Delaying search code requests, Search Request Limit reached! Delay time: {retryAfter * 500}");
                    await Task.Delay(retryAfter * 500);
                    retryCount++;
                    continue;
                }

                // Forbidden(403), check X-RateLimit-Remaining
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden &&
                    response.Headers.TryGetValues("X-RateLimit-Remaining", out IEnumerable<string> rateLimitRemaining) &&
                    rateLimitRemaining.Any())
                {
                    Console.WriteLine($"Delaying search code requests, Search Request Request Limit reached! Delay time: {60000}");
                    await Task.Delay(60000);
                    retryCount++;
                    continue;
                }

                // Bad Request, Caused by httpclient overused???
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    Console.WriteLine($"Resetting Github Httpclient!");
                    SetHttpClient();
                    retryCount++;
                    continue;
                }

                response.EnsureSuccessStatusCode();

                return response;
            }

            throw new Exception("Github Access Retry Count Exceeded.");
        }
    }
}
