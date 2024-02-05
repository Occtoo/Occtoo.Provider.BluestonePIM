using System.Text.Json.Serialization;

namespace bluestone_inbound_provider.Models
{
    public class ProductModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("number")]
        public string Number { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("attributes")]
        public Attribute[] Attributes { get; set; }
        [JsonPropertyName("media")]
        public Medium[] Media { get; set; }
        [JsonPropertyName("labels")]
        public string[] Labels { get; set; }
        [JsonPropertyName("categories")]
        public string[] Categories { get; set; }
        [JsonPropertyName("relations")]
        public Relation[] Relations { get; set; }
        [JsonPropertyName("metadata")]
        public Metadata[] Metadata { get; set; }
        [JsonPropertyName("lastUpdate")]
        public long LastUpdate { get; set; }
        [JsonPropertyName("createDate")]
        public long CreateDate { get; set; }
        [JsonPropertyName("bundles")]
        public Bundle[] Bundles { get; set; }
        [JsonPropertyName("variants")]
        public string[] Variants { get; set; }
        [JsonPropertyName("variantParentId")]
        public string VariantParentId { get; set; }
        [JsonPropertyName("relatedProductsRelationSortingOrderSource")]
        public string RelatedProductsRelationSortingOrderSource { get; set; }
        [JsonPropertyName("publishInfoId")]
        public string PublishInfoId { get; set; }
        [JsonPropertyName("contextId")]
        public string ContextId { get; set; }
    }

    public class Relation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("number")]
        public string Number { get; set; }
        [JsonPropertyName("productId")]
        public string ProductId { get; set; }
        [JsonPropertyName("reverse")]
        public bool Reverse { get; set; }
        [JsonPropertyName("direction")]
        public string Direction { get; set; }
    }

    public class Bundle
    {
        [JsonPropertyName("productId")]
        public string ProductId { get; set; }
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

}
