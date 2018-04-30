using Newtonsoft.Json;

namespace Medidata.Pikapika.Miner.Models
{
    public class PikapikaBaseClass
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
