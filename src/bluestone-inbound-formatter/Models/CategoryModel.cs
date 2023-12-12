using System.Text.Json.Serialization;

namespace bluestone_inbound_formatter.Models
{
    public class CategoriesModel
    {
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
        [JsonPropertyName("results")]
        public CategoryModel[] Results { get; set; }
    }

    public class CategoryModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("parentId")]
        public string ParentId { get; set; }
        [JsonPropertyName("order")]
        public int Order { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("number")]
        public string Number { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("media")]
        public Medium[] Media { get; set; }
        [JsonPropertyName("attributes")]
        public Attribute[] Attributes { get; set; }
        [JsonPropertyName("metaData")]
        public Metadata[] MetaData { get; set; }
    }

    public class Medium
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("downloadUri")]
        public string DownloadUri { get; set; }
        [JsonPropertyName("previewUri")]
        public string PreviewUri { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; }
        [JsonPropertyName("labels")]
        public string[] Labels { get; set; }
        [JsonPropertyName("createdAt")]
        public long CreatedAt { get; set; }
        [JsonPropertyName("updatedAt")]
        public long UpdatedAt { get; set; }
        [JsonPropertyName("number")]
        public string Number { get; set; }
    }

    public class Attribute
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("number")]
        public string Number { get; set; }
        [JsonPropertyName("unit")]
        public string Unit { get; set; }
        [JsonPropertyName("groupName")]
        public string GroupName { get; set; }
        [JsonPropertyName("groupNumber")]
        public string GroupNumber { get; set; }
        [JsonPropertyName("dataType")]
        public string DataType { get; set; }
        [JsonPropertyName("valueType")]
        public string ValueType { get; set; }
        [JsonPropertyName("definingAttribute")]
        public bool? DefiningAttribute { get; set; }
        [JsonPropertyName("values")]
        public string[] Values { get; set; }
        [JsonPropertyName("select")]
        public Select[] Select { get; set; }
        [JsonPropertyName("column")]
        public Column[] Column { get; set; }
        [JsonPropertyName("matrix")]
        public Matrix[] Matrix { get; set; }
        [JsonPropertyName("dictionary")]
        public Dictionary[] Dictionary { get; set; }
    }

    public class Select
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("number")]
        public string Number { get; set; }
        [JsonPropertyName("value")]
        public string Value { get; set; }
        [JsonPropertyName("metadata")]
        public string Metadata { get; set; }
    }

    public class Column
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class Matrix
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("rows")]
        public Row[] Rows { get; set; }
    }

    public class Row
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class Dictionary
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("number")]
        public string Number { get; set; }
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class Metadata
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

}
