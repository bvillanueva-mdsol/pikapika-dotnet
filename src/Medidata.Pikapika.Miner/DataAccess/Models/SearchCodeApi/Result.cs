using System.Collections.Generic;
using Newtonsoft.Json;

namespace Medidata.Pikapika.Miner.DataAccess.Models.SearchCodeApi
{
    public class Result
    {
        [JsonProperty("total_count")]
        public int TotalCount { get; set; }

        [JsonProperty("items")]
        public IEnumerable<ResultItem> Items { get; set; }
    }
}
