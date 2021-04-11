using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Extensions;

namespace RPDB
{
    public class RpdbSeriesProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;

        private const string RpdbBaseUrl = "http://api.ratingposterdb.com/{0}/{1}/{2}/{3}.jpg";

        internal static RpdbSeriesProvider Current { get; private set; }

        public RpdbSeriesProvider(IServerConfigurationManager config, IHttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;

            Current = this;
        }

        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "RPDB"; }
        }

        public bool Supports(BaseItem item)
        {
            return item is Series;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary, 
                ImageType.Backdrop
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            var series = (Series)item;

            var idType = "imdb";
            var seriesId = series.GetProviderId(MetadataProviders.Imdb);

            if (string.IsNullOrEmpty(seriesId))
            {
                idType = "tvdb";
                seriesId = series.GetProviderId(MetadataProviders.Tvdb);
            }

            if (!string.IsNullOrEmpty(seriesId))
            {
                try
                {
                    await AddImages(list, idType, seriesId, cancellationToken).ConfigureAwait(false);
                }
                catch (FileNotFoundException)
                {
                    // No biggie. Don't blow up
                }
                catch (IOException)
                {
                    // No biggie. Don't blow up
                }
            }

            return list;
        }

        private async Task AddImages(List<RemoteImageInfo> list, string idType, string seriesId, CancellationToken cancellationToken)
        {
            await Task.Run(() => PopulateImages(list, "poster", idType, seriesId, ImageType.Primary, 580, 859));
            await Task.Run(() => PopulateImages(list, "backdrop", idType, seriesId, ImageType.Backdrop, 1920, 1080));
        }

        private void PopulateImages(List<RemoteImageInfo> list, string reqType, string idType, string seriesId, ImageType type, int width, int height)
        {

            var clientKey = GetRpdbOptions().UserApiKey;

            if (string.IsNullOrWhiteSpace(clientKey))
            {
                return;
            }

            var posterType = "poster-default";

            if (reqType.Equals("backdrop"))
            {
                var backdrops = RpdbSeriesProvider.Current.GetRpdbOptions().Backdrops;
                if (backdrops.Equals("1"))
                {
                    if (clientKey.StartsWith("t1-") || clientKey.StartsWith("t2-"))
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
                posterType = "backdrop-default";
            }
            else if (reqType.Equals("poster"))
            {
                posterType = RpdbSeriesProvider.Current.GetRpdbOptions().PosterType;
                var textless = RpdbSeriesProvider.Current.GetRpdbOptions().Textless;
                if (textless.Equals("1"))
                {
                    if (clientKey.StartsWith("t1-") || clientKey.StartsWith("t2-"))
                    {
                        return;
                    }
                    else
                    {
                        posterType = posterType.Replace("poster-", "textless-");
                    }
                }
            }

            var url = string.Format(RpdbBaseUrl, clientKey, idType, posterType, seriesId);

            list.Add(new RemoteImageInfo
            {
                Type = type,
                Width = width,
                Height = height,
                ProviderName = Name,
                Url = url,
                Language = null
            });

        }

        public int Order
        {
            get { return 1; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }

        public RpdbOptions GetRpdbOptions()
        {
            return _config.GetConfiguration<RpdbOptions>("rpdb");
        }

    }

    public class RpdbConfigStore : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new ConfigurationStore[]
            {
                new ConfigurationStore
                {
                     Key = "rpdb",
                     ConfigurationType = typeof(RpdbOptions)
                }
            };
        }
    }
}