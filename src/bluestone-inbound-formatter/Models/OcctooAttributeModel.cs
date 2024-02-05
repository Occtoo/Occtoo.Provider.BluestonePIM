namespace bluestone_inbound_provider.Models
{
    public class OcctooAttributeModel
    {
        public string Id { get; set; } 
        public string Name { get; set; }
        public string Unit { get; set; }
        public string GroupName { get; set; }
        public string GroupNumber { get; set; }
        public string DataType { get; set; }
        public string ValueType { get; set; }
        public bool? DefiningAttribute { get; set; }
        public string Value { get; set; }
    }
}
