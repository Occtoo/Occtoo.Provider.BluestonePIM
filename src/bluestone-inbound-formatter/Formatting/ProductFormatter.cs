using bluestone_inbound_provider.Common;
using bluestone_inbound_provider.Models;
using bluestone_inbound_provider.Services;
using Newtonsoft.Json;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Attribute = bluestone_inbound_provider.Models.Attribute;

namespace bluestone_inbound_provider.Formatting
{
    public interface IProductFormatter
    {
        Task<(List<DynamicEntity>, List<DynamicEntity>)> FormatProducts(List<ProductModel> productModels, string context);
        Task<List<DynamicEntity>> FormatProductsToDelete(List<DifferenceModel> differenceModels);
        List<DynamicEntity> FormatProductsToDelete(List<ProductModel> productModels);
    }
    public class ProductFormatter : IProductFormatter
    {
        private readonly IOcctooMediaService _occtooMediaService;
        private readonly IBluestoneService _bluestoneService;

        public ProductFormatter(IOcctooMediaService occtooMediaService, IBluestoneService bluestoneService)
        {
            _occtooMediaService = occtooMediaService;
            _bluestoneService = bluestoneService;
        }

        public async Task<(List<DynamicEntity>, List<DynamicEntity>)> FormatProducts(List<ProductModel> productModels, string context)
        {
            List<DynamicEntity> productEntites = new ();
            List<DynamicEntity> mediaEntities = new ();
            if (productModels != null && productModels.Any())
            {
                var occtooProductsMedia = await GetOcctooProducts(productModels);
                if (context == "en")
                {
                    productEntites = GetProductEntities(occtooProductsMedia.Item1);
                }
                else
                {
                    productEntites = GetProductEntities(occtooProductsMedia.Item1, context);

                }
                mediaEntities = GetMediaEntities(occtooProductsMedia.Item2);
            }
            return (productEntites, mediaEntities);
        }

        public async Task<List<DynamicEntity>> FormatProductsToDelete(List<DifferenceModel> differenceModels)
        {
            var response = new List<DynamicEntity>();
            foreach (var difference in differenceModels)
            {
                var product = await _bluestoneService.GetProductById(difference.Id);
                var entity = new DynamicEntity
                {
                    Key = product.Number,
                    Delete = true
                };

                response.Add(entity);
            }

            return response;
        }

        public List<DynamicEntity> FormatProductsToDelete(List<ProductModel> productModels)
        {
            var response = new List<DynamicEntity>();
            foreach (var product in productModels)
            {
                var entity = new DynamicEntity
                {
                    Key = product.Number,
                    Delete = true
                };

                response.Add(entity);
            }

            return response;
        }

        private async Task<(List<OcctooProductModel>, List<OcctooMediaModel>)> GetOcctooProducts(List<ProductModel> productModels)
        {
            if (productModels == null || !productModels.Any())
            {
                return (null, null);
            }
            else
            {
                var products = new List<OcctooProductModel>();
                var media = new List<OcctooMediaModel>();
                foreach (var product in productModels)
                {
                    var occtooProduct = await ToOcctooProductModel(product);
                    products.Add(occtooProduct.Item1);
                    media.AddRange(occtooProduct.Item2);
                }
                return (products, media);
            }
        }

        private static List<DynamicEntity> GetProductEntities(List<OcctooProductModel> occtooProducts)
        {
            if (occtooProducts == null && !occtooProducts.Any())
            {
                return null;
            }
            else
            {
                var response = new List<DynamicEntity>();
                foreach (var product in occtooProducts)
                {
                    var entity = new DynamicEntity
                    {
                        Key = product.Number
                    };

                    var properties = typeof(OcctooProductModel).GetProperties();
                    var localizedProperties = Environment.GetEnvironmentVariable("LocalizedPropertiesProduct");
                    var localizedPropertiesArray = localizedProperties.Split(',').Select(s => s.Trim()).ToArray();
                    var selectedProperties = properties
                        .Where(prop => localizedProperties.Contains(prop.Name))
                        .ToArray();
                    var otherProperties = properties
                        .Where(prop => !localizedProperties.Contains(prop.Name))
                        .ToArray();

                    foreach (var property in selectedProperties)
                    {
                        entity.Properties.Add(new DynamicProperty
                        {
                            Id = property.Name,
                            Language = "en",
                            Value = property.GetValue(product)?.ToString()
                        });
                    }
                    foreach (var property in otherProperties)
                    {
                        switch (property.Name)
                        {
                            case "Relations":
                                if (property.GetValue(product) is List<OcctooRelationModel> relations && relations.Any())
                                {
                                    foreach (var relation in relations)
                                    {
                                        entity.Properties.Add(new DynamicProperty
                                        {
                                            Id = $"relation-{relation.Number}-productId",
                                            Value = relation.ProductId
                                        });
                                    }
                                }

                                break;
                            case "Attributes":
                                if (property.GetValue(product) is List<OcctooAttributeModel> attributes && attributes.Any())
                                {
                                    foreach (var attribute in attributes)
                                    {
                                        if (localizedPropertiesArray.Any(x => x == attribute.Id))
                                        {
                                            entity.Properties.Add(new DynamicProperty
                                            {
                                                Id = attribute.Id,
                                                Language = "en",
                                                Value = attribute.Value?.ToString()
                                            });
                                        }
                                        else
                                        {
                                            entity.Properties.Add(new DynamicProperty
                                            {
                                                Id = attribute.Id,
                                                Value = attribute.Value?.ToString()
                                            });
                                        }
                                    }
                                }
                                break;
                            case "Media":
                                if (property.GetValue(product) is List<string> media && media.Any())
                                    entity.Properties.Add(new DynamicProperty
                                    {
                                        Id = "media",
                                        Value = string.Join("|", media)
                                    });
                                break;
                            default:
                                entity.Properties.Add(new DynamicProperty
                                {
                                    Id = property.Name,
                                    Value = property.GetValue(product)?.ToString()
                                });
                                break;

                        }

                    }
                    response.Add(entity);
                }

                response = response.GroupBy(entity => entity.Key).Select(group => group.First()).ToList();

                return response;
            }
        }

        private static List<DynamicEntity> GetProductEntities(List<OcctooProductModel> occtooProducts, string context)
        {
            if (occtooProducts == null && !occtooProducts.Any())
            {
                return null;
            }
            else
            {
                var response = new List<DynamicEntity>();
                foreach (var product in occtooProducts)
                {
                    var entity = new DynamicEntity
                    {
                        Key = product.Number
                    };

                    var properties = typeof(OcctooProductModel).GetProperties();
                    var localizedProperties = Environment.GetEnvironmentVariable("LocalizedPropertiesProduct");
                    var localizedPropertiesArray = localizedProperties.Split(',').Select(s => s.Trim()).ToArray();
                    var selectedProperties = properties
                        .Where(prop => localizedProperties.Contains(prop.Name))
                        .ToArray();
                    var otherProperties = properties
                        .Where(prop => !localizedProperties.Contains(prop.Name))
                        .ToArray();

                    foreach (var property in selectedProperties)
                    {
                        entity.Properties.Add(new DynamicProperty
                        {
                            Id = property.Name,
                            Language = context,
                            Value = property.GetValue(product)?.ToString()
                        });

                    }
                    foreach (var property in otherProperties)
                    {
                        switch (property.Name)
                        {
                            case "Relations":
                                if (property.GetValue(product) is List<OcctooRelationModel> relations && relations.Any())
                                {
                                    foreach (var relation in relations)
                                    {
                                        if (localizedPropertiesArray.Any(x => x == relation.Number))
                                        {
                                            entity.Properties.Add(new DynamicProperty
                                            {
                                                Id = $"relation-{relation.Number}-productId",
                                                Language = context,
                                                Value = relation.ProductId
                                            });
                                        }
                                    }
                                }

                                break;
                            case "Attributes":
                                if (property.GetValue(product) is List<OcctooAttributeModel> attributes && attributes.Any())
                                {
                                    foreach (var attribute in attributes)
                                    {
                                        if (localizedPropertiesArray.Any(x => x == attribute.Id))
                                        {
                                            entity.Properties.Add(new DynamicProperty
                                            {
                                                Id = attribute.Id,
                                                Language = context,
                                                Value = attribute.Value?.ToString()
                                            });
                                        }
                                    }
                                }
                                break;

                        }

                    }

                    response.Add(entity);
                }

                response = response.GroupBy(entity => entity.Key).Select(group => group.First()).ToList();

                return response;
            }
        }

        private static List<DynamicEntity> GetMediaEntities(List<OcctooMediaModel> occtooMediaModels)
        {
            if (occtooMediaModels == null && !occtooMediaModels.Any())
            {
                return null;
            }
            else
            {
                var response = new List<DynamicEntity>();
                foreach (var media in occtooMediaModels)
                {
                    var entity = new DynamicEntity
                    {
                        Key = media.Id
                    };

                    var properties = typeof(OcctooMediaModel).GetProperties();

                    foreach (var property in properties)
                    {
                        entity.Properties.Add(new DynamicProperty
                        {
                            Id = property.Name,
                            Value = property.GetValue(media)?.ToString()
                        });
                    }

                    response.Add(entity);
                }

                response = response.GroupBy(entity => entity.Key).Select(group => group.First()).ToList();

                return response;
            }
        }

        private async Task<(OcctooProductModel, List<OcctooMediaModel>)> ToOcctooProductModel(ProductModel productModel)
        {
            var occtooProductModel = new OcctooProductModel
            {
                Id = productModel.Id,
                ProductType = productModel.Type,
                Name = productModel.Name,
                Number = productModel.Number,
                Description = productModel.Description,
                Attributes = GetAttributes(productModel.Attributes),
                Labels = productModel.Labels != null && productModel.Labels.Any() ? string.Join("|", productModel.Labels) : string.Empty,
                Categories = productModel.Categories != null && productModel.Categories.Any() ? string.Join("|", productModel.Categories) : string.Empty,
                Relations = GetRelations(productModel.Relations),
                Metadata = GetMetadata(productModel.Metadata),
                LastUpdate = UnixHelper.UnixTimeStampToDateTime(productModel.LastUpdate),
                CreateDate = UnixHelper.UnixTimeStampToDateTime(productModel.CreateDate),
                BundleProductIds = GetBundleProductIds(productModel.Bundles),
                BundleQuantites = GetBundleQuantites(productModel.Bundles),
                Variants = productModel.Variants != null && productModel.Variants.Any() ? string.Join("|", productModel.Variants) : string.Empty,
                VariantParentId = productModel.VariantParentId,
                RelatedProductsRelationSortingOrderSource = productModel.RelatedProductsRelationSortingOrderSource,
                PublishInfoId = productModel.PublishInfoId,
                ContextId = productModel.ContextId
            };

            var occtooMediaModels = await GetMedia(productModel.Media);
            occtooProductModel.Media = occtooMediaModels.Select(x => x.Id).ToList();
            occtooProductModel.Thumbnail = occtooMediaModels.FirstOrDefault()?.Thumbnail;
            return (occtooProductModel, occtooMediaModels);
        }

        private async Task<List<OcctooMediaModel>> GetMedia(Medium[] media)
        {
            var occtooMediaModels = new List<OcctooMediaModel>();
            var handleMedia = Environment.GetEnvironmentVariable("HandleMedia");
            if (handleMedia == "occtoo")
            {
                foreach (var mediaItem in media)
                {
                    var occtooMedia =  await _occtooMediaService.GetMedia(mediaItem);
                    occtooMediaModels.Add(occtooMedia);
                }
            }
            else if (handleMedia == "bluestone")
            {
                foreach (var mediaItem in media)
                {
                    var occtooMedia = new OcctooMediaModel
                    {
                        Id = mediaItem.Id,
                        DownloadUri = mediaItem.DownloadUri,
                        PreviewUri = mediaItem.PreviewUri,
                        Thumbnail = mediaItem.PreviewUri,
                        Name = mediaItem.Name,
                        Description = mediaItem.Description,
                        FileName = mediaItem.FileName,
                        ContentType = mediaItem.ContentType,
                        Labels = mediaItem.Labels != null && mediaItem.Labels.Any() ? string.Join("|", mediaItem.Labels) : string.Empty,
                        CreatedAt = UnixHelper.UnixTimeStampToDateTime(mediaItem.CreatedAt),
                        UpdatedAt = UnixHelper.UnixTimeStampToDateTime(mediaItem.UpdatedAt),
                        Number = mediaItem.Number
                    };
                    occtooMediaModels.Add(occtooMedia);
                }
            }
            
            return occtooMediaModels;
        }

        private static List<OcctooRelationModel> GetRelations(Relation[] relations)
        {
            var occtooRelations = new List<OcctooRelationModel>();
            var count = 1;
            foreach (var relation in relations)
            {
                var occtooRelation = new OcctooRelationModel
                {
                    Number = $"{relation.Number}{count}",
                    ProductId = relation.ProductId
                };
                count++;
                occtooRelations.Add(occtooRelation);
            }
            return occtooRelations;
        }

        private static string GetMetadata(Metadata[] metadata)
        {
            var res = string.Empty;
            if (metadata != null && metadata.Length > 0)
            {
                string[] metas = metadata.Select(m => m.Value).ToArray();
                res = string.Join(" | ", metas);
            }
            return res;
        }

        private static string GetBundleQuantites(Bundle[] bundles)
        {
            var res = string.Empty;
            if (bundles != null && bundles.Length > 0)
            {
                string[] productIds = bundles.Select(b => b.Quantity.ToString()).ToArray();
                res = string.Join(" | ", productIds);
            }
            return res;

        }

        private static string GetBundleProductIds(Bundle[] bundles)
        {
            var res = string.Empty;
            if (bundles != null && bundles.Length > 0)
            {
                string[] productIds = bundles.Select(b => b.ProductId).ToArray();
                res = string.Join(" | ", productIds);
            }
            return res;
        }

        private static List<OcctooAttributeModel> GetAttributes(Attribute[] attributes)
        {
            var occtooAttributes = new List<OcctooAttributeModel>();
            foreach (var attribute in attributes)
            {
                var occtooAttribute = new OcctooAttributeModel
                {
                    Id = attribute.Number,
                    Name = $"{attribute.GroupName} {attribute.Name}",
                    Unit = attribute.Unit,
                    GroupName = attribute.GroupName,
                    GroupNumber = attribute.GroupNumber,
                    DataType = attribute.DataType,
                    ValueType = attribute.ValueType,
                    DefiningAttribute = attribute.DefiningAttribute
                };

                switch (attribute.DataType)
                {
                    case "text":
                    case "formatted_text":
                    case "boolean":
                    case "date":
                    case "date_time":
                    case "decimal":
                    case "integer":
                    case "time":
                    case "pattern":
                        occtooAttribute.Value = GetValue(attribute.Values);
                        break;
                    case "single_select":
                    case "multi_select":
                        occtooAttribute.Value = GetSelectValue(attribute.Select);
                        var occtooAttributeSelectKey = new OcctooAttributeModel
                        {
                            Id = $"{attribute.Number}Key",
                            Name = $"{attribute.GroupName} {attribute.Name}",
                            Unit = attribute.Unit,
                            GroupName = attribute.GroupName,
                            GroupNumber = attribute.GroupNumber,
                            DataType = attribute.DataType,
                            ValueType = attribute.ValueType,
                            DefiningAttribute = attribute.DefiningAttribute,
                            Value = GetSelectKey(attribute.Select)
                        };
                        occtooAttributes.Add(occtooAttributeSelectKey);
                        break;
                    case "column":
                        occtooAttribute.Value = GetColumnValue(attribute.Column);
                        break;
                    case "dictionary":
                        occtooAttribute.Value = GetDictionaryValue(attribute.Dictionary);
                        var occtooAttributeDictionaryKey = new OcctooAttributeModel
                        {
                            Id = $"{attribute.Number}Key",
                            Name = $"{attribute.GroupName} {attribute.Name}",
                            Unit = attribute.Unit,
                            GroupName = attribute.GroupName,
                            GroupNumber = attribute.GroupNumber,
                            DataType = attribute.DataType,
                            ValueType = attribute.ValueType,
                            DefiningAttribute = attribute.DefiningAttribute,
                            Value = GetDictionaryKey(attribute.Dictionary)
                        };
                        occtooAttributes.Add(occtooAttributeDictionaryKey);
                        break;
                    case "matrix":
                        occtooAttribute.Value = GetMatrixValue(attribute.Matrix);
                        break;
                }

                occtooAttributes.Add(occtooAttribute);
            }
            return occtooAttributes;
        }

        private static string GetSelectKey(Select[] select)
        {
            var res = string.Empty;
            if (select != null && select.Length > 0)
            {
                string[] keys = select.Select(x => x.Number).ToArray();
                res = string.Join(" | ", keys);
            }
            return res;

        }

        private static string GetSelectValue(Select[] values)
        {
            var res = string.Empty;
            if (values != null && values.Length > 0)
            {
                string[] vals = values.Select(v => v.Value).ToArray();
                res = string.Join(" | ", vals);
            }
            return res;
        }

        private static string GetValue(string[] values)
        {
            return values != null && values.Any() ? string.Join("|", values) : string.Empty;
        }

        private static string GetMatrixValue(Matrix[] matrix)
        {
            return JsonConvert.SerializeObject(matrix, Newtonsoft.Json.Formatting.Indented);
        }

        private static string GetDictionaryValue(Dictionary[] dictionary)
        {
            var res = string.Empty;
            if (dictionary != null && dictionary.Length > 0)
            {
                string[] values = dictionary.Select(x => x.Value).ToArray();
                res = string.Join(" | ", values);
            }
            return res;
        }

        private static string GetDictionaryKey(Dictionary[] dictionary)
        {
            var res = string.Empty;
            if (dictionary != null && dictionary.Length > 0)
            {
                string[] keys = dictionary.Select(x => x.Number).ToArray();
                res = string.Join(" | ", keys);
            }
            return res;
        }

        private static string GetColumnValue(Column[] column)
        {
            return JsonConvert.SerializeObject(column, Newtonsoft.Json.Formatting.Indented);
        }

    }
}
