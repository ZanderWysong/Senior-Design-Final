using System.Collections.Generic;

namespace ZitaDataSystem.Models
{
    public class RequestPayload
    {
        public List<Dictionary<string, string>> rows { get; set; }
    }
}
