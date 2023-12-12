using System.Text.Json.Serialization;

namespace bluestone_inbound_formatter.Models
{
    public class SyncModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("state")]
        public string State { get; set; }
        [JsonPropertyName("createdAt")]
        public long CreatedAt { get; set; }
        [JsonPropertyName("disposedAt")]
        public long DisposedAt { get; set; }
        [JsonPropertyName("contextId")]
        public string ContextId { get; set; }
        [JsonPropertyName("contextName")]
        public string ContextName { get; set; }
    }
}
