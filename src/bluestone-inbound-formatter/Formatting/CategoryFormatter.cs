using bluestone_inbound_provider.Models;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace bluestone_inbound_provider.Formatting
{
    public interface ICategoryFormatter
    {
        List<DynamicEntity> FormatCategories(List<CategoryModel> categoriesModels, KeyValuePair<string, string> context);
        List<DynamicEntity> FormatCategoriesDelete(List<DifferenceModel> differenceModels);
    }
    public class CategoryFormatter : ICategoryFormatter
    {      

        public List<DynamicEntity> FormatCategories(List<CategoryModel> categoriesModels, KeyValuePair<string, string> context)
        {
            var occtooCategories = GetOcctooCategories(categoriesModels);
            var categoriesEntites = GetCategoryEntities(occtooCategories, context);
            return categoriesEntites;

        }
        public List<DynamicEntity> FormatCategoriesDelete(List<DifferenceModel> differenceModels)
        {
            var response = new List<DynamicEntity>();
            foreach (var difference in differenceModels)
            {
                var entity = new DynamicEntity
                {
                    Key = difference.Id,
                    Delete = true
                };

                response.Add(entity);
            }

            return response;
        }

        private static string GetLocalizedProperties()
        {
            var localizedProperties = Environment.GetEnvironmentVariable("LocalizedPropertiesCategory");
            var localizedPropertiesArray = localizedProperties.Split(',').Select(s => s.Trim()).ToArray();
            return localizedProperties;
        }
        private static List<DynamicEntity> GetCategoryEntities(List<OcctooCategoryModel> occtooCategories, KeyValuePair<string, string> context)
        {
            if (occtooCategories == null && !occtooCategories.Any())
            {
                return null;
            }
            else
            {
                List<DynamicEntity> response;

                if (context.Key == "en")
                {
                    response = GetCategories(occtooCategories);
                }
                else
                {
                    response = GetCategories(occtooCategories, context);
                }

                return response;
            }
        }
        private static List<DynamicEntity> GetCategories(List<OcctooCategoryModel> occtooCategories, KeyValuePair<string, string> context)
        {
            var response = new List<DynamicEntity>();
            foreach (var category in occtooCategories)
            {
                var entity = new DynamicEntity
                {
                    Key = category.Id
                };

                var properties = typeof(OcctooCategoryModel).GetProperties();
                var localizedProperties = Environment.GetEnvironmentVariable("LocalizedPropertiesCategory");
                var localizedPropertiesArray = localizedProperties.Split(',').Select(s => s.Trim()).ToArray();
                var selectedProperties = properties
                    .Where(prop => localizedProperties.Contains(prop.Name))
                    .ToArray();

                foreach (var property in selectedProperties)
                {
                    entity.Properties.Add(new DynamicProperty
                    {
                        Id = property.Name,
                        Language = context.Value,
                        Value = property.GetValue(category)?.ToString()
                    });
                }
                response.Add(entity);
            }

            response = response.GroupBy(entity => entity.Key).Select(group => group.First()).ToList();

            return response;
        }
        private static List<DynamicEntity> GetCategories(List<OcctooCategoryModel> occtooCategories)
        {
            var properties = typeof(OcctooCategoryModel).GetProperties();
            string localizedProperties = GetLocalizedProperties();
            var selectedProperties = properties
                .Where(prop => localizedProperties.Contains(prop.Name))
                .ToArray();
            var otherProperties = properties
                .Where(prop => !localizedProperties.Contains(prop.Name))
                .ToArray();

            var response = new List<DynamicEntity>();
            foreach (var category in occtooCategories)
            {
                var entity = new DynamicEntity
                {
                    Key = category.Id
                };

                foreach (var property in selectedProperties)
                {
                    entity.Properties.Add(new DynamicProperty
                    {
                        Id = property.Name,
                        Language = "en",
                        Value = property.GetValue(category)?.ToString()
                    });
                }

                foreach (var property in otherProperties)
                {
                    entity.Properties.Add(new DynamicProperty
                    {
                        Id = property.Name,
                        Value = property.GetValue(category)?.ToString()
                    });
                }
                response.Add(entity);
            }

            response = response.GroupBy(entity => entity.Key).Select(group => group.First()).ToList();
            return response;
        }
        private static List<OcctooCategoryModel> GetOcctooCategories(List<CategoryModel> categoriesModels)
        {
            if (categoriesModels == null || !categoriesModels.Any())
            {
                return null;
            }
            else
            {
                var categories = new List<OcctooCategoryModel>();
                foreach (var category in categoriesModels)
                {
                    var occtooCategory = OcctooCategoryModel.ToOcctooCategoryModel(category);
                    categories.Add(occtooCategory);
                }
                return categories;
            }
        }

    }
}
