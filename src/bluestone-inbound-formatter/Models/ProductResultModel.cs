using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace bluestone_inbound_provider.Models
{
    public class ProductResultModel
    {
        [JsonPropertyName("nextCursor")]
        public string NextCursor { get; set; }

        [JsonPropertyName("results")]
        public List<ProductModel> Results { get; set; }
    }
}
