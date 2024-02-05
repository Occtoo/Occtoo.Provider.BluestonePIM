using bluestone_inbound_provider.Common;
using bluestone_inbound_provider.Models;
using Occtoo.Onboarding.Sdk;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace bluestone_inbound_provider.Services
{
    public interface IOcctooMediaService
    {
        Task<OcctooMediaModel> GetMedia(Medium mediaItem);

    }

    public class OcctooMediaService : IOcctooMediaService
    {
        private readonly ITokenService _tokenService;
        private readonly OnboardingServiceClient _mediaClient;
        private readonly string _dataProviderId;
        private readonly string _dataProviderSecret;


        public OcctooMediaService(ITokenService tokenService)
        {
            _dataProviderId = Environment.GetEnvironmentVariable("DataProviderId");
            _dataProviderSecret = Environment.GetEnvironmentVariable("DataProviderSecret");
            _mediaClient = new OnboardingServiceClient(_dataProviderId, _dataProviderSecret);
            _tokenService = tokenService;
        }

        public async Task<OcctooMediaModel> GetMedia(Medium mediaItem)
        {
            var occtooMediaModel = new OcctooMediaModel();
            var uniqueIdentifier = CreateIdentifier(mediaItem.DownloadUri, "url" + mediaItem.FileName);
            var originalUrl = mediaItem.DownloadUri;
            var mediaFromUrl = await UploadFileToOcctooMediaService(originalUrl, mediaItem.FileName, uniqueIdentifier);
            if (mediaFromUrl != null)
            {
                occtooMediaModel = new OcctooMediaModel
                {
                    Id = mediaItem.Id,
                    FileName = mediaFromUrl.Metadata.Filename,
                    DownloadUri = mediaFromUrl.PublicUrl,
                    PreviewUri = mediaFromUrl.PublicUrl,
                    Thumbnail = $"{mediaFromUrl.PublicUrl}&format=small",
                    Description = mediaItem.Description,
                    Name = mediaItem.Name,
                    ContentType = mediaItem.ContentType,
                    Labels = mediaItem.Labels != null && mediaItem.Labels.Any() ? string.Join("|", mediaItem.Labels) : string.Empty,
                    Number = mediaItem.Number,
                    CreatedAt = UnixHelper.UnixTimeStampToDateTime(mediaItem.CreatedAt),
                    UpdatedAt= UnixHelper.UnixTimeStampToDateTime(mediaItem.UpdatedAt)
                };

            }
            return occtooMediaModel;
        }

        private async Task<MediaFileDto> UploadFileToOcctooMediaService(string url, string filename, string uniqueIdentifier)
        {
            try
            {
                var token = await _tokenService.GetCachedToken(_dataProviderId, _dataProviderSecret, "OcctooToken");
                var fileToUpload = new FileUploadFromLink(
                            url,
                            filename,
                            uniqueIdentifier);

                var cancellationToken = new CancellationTokenSource(180000).Token; // 3 mins               
                var response = await _mediaClient.UploadFromLinkAsync(fileToUpload, token, cancellationToken);
                if (response.StatusCode == 200)
                {
                    MediaFileDto uploadDto = response.Result;
                    return uploadDto;
                }
                else
                {
                    throw new Exception($"There was a problem uploading media {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static string CreateIdentifier(string url, string filename) => string.Concat(url.AsSpan(url.LastIndexOf("=") + 1), "_", filename);

    }
}
