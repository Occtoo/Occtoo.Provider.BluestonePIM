using System.Collections.Generic;

namespace bluestone_inbound_formatter.Models
{
    public class OcctooProductModel
    {
        public string Id { get; set; }
        public string ProductType { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
        public string Description { get; set; }
        public List<OcctooAttributeModel> Attributes { get; set; }
        public List<string> Media { get; set; }
        public string Labels { get; set; }
        public string Categories { get; set; }
        public List<OcctooRelationModel> Relations { get; set; }
        public string Metadata { get; set; }
        public string LastUpdate { get; set; }
        public string CreateDate { get; set; }
        public string BundleProductIds { get; set; }
        public string BundleQuantites { get; set; }
        public string Variants { get; set; }
        public string VariantParentId { get; set; }
        public string RelatedProductsRelationSortingOrderSource { get; set; }
        public string PublishInfoId { get; set; }
        public string ContextId { get; set; }    
        public string Thumbnail { get; set; }
    }
}
