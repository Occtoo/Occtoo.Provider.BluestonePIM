namespace bluestone_inbound_formatter.Models
{
    public  class OcctooCategoryModel
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public int Order { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
        public string Description { get; set; }

        public static OcctooCategoryModel ToOcctooCategoryModel(CategoryModel categoryModel)
        {
            var occtooCategoryModel = new OcctooCategoryModel
            {
                Id = categoryModel.Id,
                ParentId = categoryModel.ParentId,
                Order = categoryModel.Order,
                Name = categoryModel.Name,
                Number = categoryModel.Number,
                Description = categoryModel.Description,
            };

            return occtooCategoryModel;
        }
    }


}
