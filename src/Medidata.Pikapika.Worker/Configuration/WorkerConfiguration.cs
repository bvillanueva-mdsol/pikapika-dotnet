using System.Collections.Generic;

namespace Medidata.Pikapika.Worker.Configuration
{
    public class WorkerConfiguration
    {
        public ConnectionStrings ConnectionStrings { get; set; }

        public string AuthorizationUsername { get; set; }

        public string AuthorizationToken { get; set; }

        public string GithubBaseUri { get; set; }

        public string PublicNugetServerUri { get; set; }

        public string MedidataNugetServerBaseUri { get; set; }

        public string MedidataNugetAccessUserName { get; set; }

        public string MedidataNugetAccessPassword { get; set; }

        public IEnumerable<string> MedidataRepositories { get; set; }
    }
}
