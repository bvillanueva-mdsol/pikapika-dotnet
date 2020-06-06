using Medidata.Pikapika.Miner.DataAccess.Models.SearchCodeApi;
using Medidata.Pikapika.Miner.Models;

namespace Medidata.Pikapika.Miner.Extensions
{
    public static class ResultItemExtenstions
    {
        public static DotnetAppProjectFile ConvertToDotnetAppProjectFile(this ResultItem resultItem)
        {
            return new DotnetAppProjectFile
            {
                ProjectFileName = resultItem.Name,
                ProjectFilePath = resultItem.Path,
                ProjectContentsUrl = resultItem.ContentsUrl
            };
        }
    }
}
