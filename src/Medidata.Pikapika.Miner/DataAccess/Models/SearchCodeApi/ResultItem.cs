using Newtonsoft.Json;

namespace Medidata.Pikapika.Miner.DataAccess.Models.SearchCodeApi
{
    public class ResultItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("url")]
        public string ContentsUrl { get; set; }
    }
}
