using System.Text.Json.Serialization;

namespace bluestone_inbound_provider.Models
{
    public class DifferenceModel
    {
        [JsonPropertyName("diffType")]
        public string DiffType { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

}
