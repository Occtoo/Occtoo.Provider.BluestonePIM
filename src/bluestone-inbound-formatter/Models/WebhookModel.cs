using System.Text.Json.Serialization;

namespace bluestone_inbound_formatter.Models
{

    public class WebhookModel
    {
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
        [JsonPropertyName("events")]
        public Event[] Events { get; set; }
    }

    public class Event
    {
        [JsonPropertyName("changes")]
        public Changes Changes { get; set; }
    }

    public class Changes
    {
        [JsonPropertyName("eventType")]
        public string EventType { get; set; }
        [JsonPropertyName("entityIds")]
        public object[] EntityIds { get; set; }
        [JsonPropertyName("syncDoneData")]
        public Syncdonedata SyncDoneData { get; set; }
        [JsonPropertyName("stateChange")]
        public Statechange StateChange { get; set; }
    }

    public class Syncdonedata
    {
        [JsonPropertyName("field")]
        public string Field { get; set; }
        [JsonPropertyName("value")]
        public string Value { get; set; }
        [JsonPropertyName("context")]
        public string Context { get; set; }
    }

    public class Statechange
    {
        [JsonPropertyName("changeType")]
        public string ChangeType { get; set; }
        [JsonPropertyName("oldValue")]
        public object OldValue { get; set; }
        [JsonPropertyName("newValue")]
        public string NewValue { get; set; }
        [JsonPropertyName("context")]
        public string Context { get; set; }
    }



}
