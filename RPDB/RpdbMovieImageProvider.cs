using System.Net;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;
using System.Net.Http;

namespace RPDB
{
    public class RpdbMovieImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IHttpClient _httpClient;

        private const string RpdbBaseUrl = "http://api.ratingposterdb.com/{0}/{1}/{2}/{3}.jpg{4}";
        private const string BaseTmdbId = "movie-{0}";

        internal static RpdbMovieImageProvider Current;

        public RpdbMovieImageProvider(IHttpClient httpClient)
        {
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
            return item is Movie;
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
            var baseItem = item;
            var list = new List<RemoteImageInfo>();

            var idType = "imdb";
            var movieId = baseItem.GetProviderId(MetadataProviders.Imdb);

            if (string.IsNullOrEmpty(movieId))
            {
                idType = "tmdb";
                movieId = string.Format(BaseTmdbId, baseItem.GetProviderId(MetadataProviders.Tmdb));
            }

            if (!string.IsNullOrEmpty(movieId))
            {
                try
                {
                    await AddImages(list, idType, movieId, cancellationToken).ConfigureAwait(false);
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

        private async Task AddImages(List<RemoteImageInfo> list, string idType, string movieId, CancellationToken cancellationToken)
        {
            await Task.Run(() => PopulateImages(list, "poster", idType, movieId, ImageType.Primary, 580, 859));
            await Task.Run(() => PopulateImages(list, "backdrop", idType, movieId, ImageType.Backdrop, 1920, 1080));
        }

        private void PopulateImages(List<RemoteImageInfo> list, string reqType, string idType, string movieId, ImageType type, int width, int height)
        {

            var clientKey = RpdbSeriesProvider.Current.GetRpdbOptions().UserApiKey;

            if (string.IsNullOrWhiteSpace(clientKey))
            {
                return;
            }

            var posterType = "poster-default";
            var fallback = "";
            var posterLang = RpdbSeriesProvider.Current.GetRpdbOptions().PosterLang;

            if (reqType.Equals("backdrop"))
            {
                var backdrops = RpdbSeriesProvider.Current.GetRpdbOptions().Backdrops;
                if (backdrops.Equals("1"))
                {
                    if (clientKey.StartsWith("t0-") || clientKey.StartsWith("t1-"))
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
                fallback = "?fallback=true";
                if (!clientKey.StartsWith("t0-")) {
                    posterType = RpdbSeriesProvider.Current.GetRpdbOptions().PosterType;
                }
                if (!posterLang.Equals("en") && !clientKey.StartsWith("t0-") && !clientKey.StartsWith("t1-"))
                {
                    fallback += "&lang=";
                    fallback += posterLang;
                }
                if (posterType.Equals("rating-order") && !clientKey.StartsWith("t0-") && !clientKey.StartsWith("t1-") && !clientKey.StartsWith("t2-"))
                {
                    fallback += "&order=";
                    var firstRating = RpdbSeriesProvider.Current.GetRpdbOptions().FirstRating; 
                    fallback += firstRating;
                    var secondRating = RpdbSeriesProvider.Current.GetRpdbOptions().SecondRating; 
                    if (!secondRating.Equals("none"))
                    {
                        fallback += "%2C";
                        fallback += secondRating;
                    }
                    var thirdRating = RpdbSeriesProvider.Current.GetRpdbOptions().ThirdRating; 
                    if (!thirdRating.Equals("none"))
                    {
                        fallback += "%2C";
                        fallback += thirdRating;
                    }
                    var firstBackupRating = RpdbSeriesProvider.Current.GetRpdbOptions().FirstBackupRating; 
                    if (!firstBackupRating.Equals("none"))
                    {
                        fallback += "%2C";
                        fallback += firstBackupRating;
                    }
                    var secondBackupRating = RpdbSeriesProvider.Current.GetRpdbOptions().SecondBackupRating;             
                    if (!secondBackupRating.Equals("none"))
                    {
                        fallback += "%2C";
                        fallback += secondBackupRating;
                    }
                }
                else
                {
                    var textless = RpdbSeriesProvider.Current.GetRpdbOptions().Textless;
                    if (textless.Equals("1") && !clientKey.StartsWith("t0-"))
                    {
                        posterType = posterType.Replace("poster-", "textless-");
                    }
                }
            }

            var url = string.Format(RpdbBaseUrl, clientKey, idType, posterType, movieId, fallback);

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

        public async Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                EnableDefaultUserAgent = true,
                TimeoutMs = 30000
            };

            var response = await _httpClient.GetResponse(options).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException($"Bad Status Code: {response.StatusCode}");

            return response;
        }
    }
}