using System.Net;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
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
                    await AddImages(item, list, idType, movieId, cancellationToken).ConfigureAwait(false);
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

        private async Task AddImages(BaseItem item, List<RemoteImageInfo> list, string idType, string movieId, CancellationToken cancellationToken)
        {
            await Task.Run(() => PopulateImages(item, list, "poster", idType, movieId, ImageType.Primary, 580, 859));
            await Task.Run(() => PopulateImages(item, list, "backdrop", idType, movieId, ImageType.Backdrop, 1920, 1080));
        }

        private void PopulateImages(BaseItem item, List<RemoteImageInfo> list, string reqType, string idType, string movieId, ImageType type, int width, int height)
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

            var videoQuality = RpdbSeriesProvider.Current.GetRpdbOptions().VideoQuality;
            var video3D = RpdbSeriesProvider.Current.GetRpdbOptions().Video3D;
            var colorRange = RpdbSeriesProvider.Current.GetRpdbOptions().ColorRange;
            var audioChannels = RpdbSeriesProvider.Current.GetRpdbOptions().AudioChannels;
            var videoCodec = RpdbSeriesProvider.Current.GetRpdbOptions().VideoCodec;

            var addedVideoQuality = false;
            var addedVideo3d = false;
            var addedColorRange = false;
            var addedVideoCodec = false;
            var addedAudioChannels = false;

            if (videoQuality.Equals("1") || video3D.Equals("1") || colorRange.Equals("1") || audioChannels.Equals("1") || videoCodec.Equals("1")) {
                var hasMediaSources = item as IHasMediaSources;

                if (hasMediaSources != null)
                {
                    var mediaStreams = hasMediaSources.GetMediaStreams();

                    var badgeString = "";

                    foreach (var stream in mediaStreams)
                    {
                        if (stream.Type.Equals(MediaStreamType.Video))
                        {
                            if (videoQuality.Equals("1") && !addedVideoQuality)
                            {
                                var resBadge = "";
                                if (stream.Width.HasValue)
                                {
                                    if (stream.Width.Equals(852) || (stream.Width > 852 && stream.Width < 1280) || stream.Width < 852) {
                                        resBadge = "480p";
                                    }
                                    else if (stream.Width.Equals(1280) || (stream.Width > 1280 && stream.Width < 1920) || (stream.Width < 1280 && stream.Width > 852)) {
                                        resBadge = "720p";
                                    }
                                    else if (stream.Width.Equals(1920) || (stream.Width > 1920 && stream.Width < 2048) || (stream.Width < 1920 && stream.Width > 1280)) {
                                        resBadge = "1080p";
                                    }
                                    else if (stream.Width.Equals(2048) || (stream.Width > 2048 && stream.Width < 3840) || (stream.Width < 2048 && stream.Width > 1920)) {
                                        resBadge = "2k";
                                    }
                                    else if (stream.Width.Equals(3840) || (stream.Width > 3840 && stream.Width < 5120) || (stream.Width < 3840 && stream.Width > 2048)) {
                                        resBadge = "4k";
                                    }
                                    else if (stream.Width.Equals(5120) || (stream.Width > 5120 && stream.Width < 7680) || (stream.Width < 5120 && stream.Width > 3840)) {
                                        resBadge = "5k";
                                    }
                                    else if (stream.Width.Equals(7680) || stream.Width > 7680 || (stream.Width < 7680 && stream.Width > 5120)) {
                                        resBadge = "8k";
                                    }
                                    if (!resBadge.Equals("")) {
                                        if (!badgeString.Equals("")) {
                                            badgeString += "%2C";
                                        }
                                        badgeString += resBadge;
                                        addedVideoQuality = true;
                                    }
                                }
                            }

                            if (video3D.Equals("1") && !addedVideo3d) {
                                var video3dBadge = "";
                                var video = item as Video;
                                if (video != null && video.Video3DFormat.HasValue) {
                                    video3dBadge = "3d";
                                }
                                if (!video3dBadge.Equals("")) {
                                    if (!badgeString.Equals("")) {
                                        badgeString += "%2C";
                                    }
                                    badgeString += video3dBadge;
                                    addedVideo3d = true;
                                }
                            }

                            if (colorRange.Equals("1") && !addedColorRange) {
                                var colorRangeBadge = "";
                                if (!stream.ExtendedVideoType.Equals(ExtendedVideoTypes.None))
                                {
                                    if (stream.ExtendedVideoType.Equals(ExtendedVideoTypes.DolbyVision)) {
                                        colorRangeBadge = "dolbyvisioncolor";
                                    }
                                    else if (stream.ExtendedVideoType.Equals(ExtendedVideoTypes.Hdr10) || stream.ExtendedVideoType.Equals(ExtendedVideoTypes.Hdr10Plus)) {
                                        colorRangeBadge = "hdrcolor";
                                    }
                                }
                                if (!colorRangeBadge.Equals("")) {
                                    if (!badgeString.Equals("")) {
                                        badgeString += "%2C";
                                    }
                                    badgeString += colorRangeBadge;
                                    addedColorRange = true;
                                }
                            }

                            if (videoCodec.Equals("1") && !addedVideoCodec) {
                                if (!string.IsNullOrEmpty(stream.Codec))
                                {
                                    var videoCodecBadge = "";
                                    var codec = stream.Codec;
                                    if (string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase) || string.Equals(codec, "h265", StringComparison.OrdinalIgnoreCase)) {
                                        videoCodecBadge = "h265";
                                    } else if (string.Equals(codec, "avc", StringComparison.OrdinalIgnoreCase) || string.Equals(codec, "h264", StringComparison.OrdinalIgnoreCase)) {
                                        videoCodecBadge = "h264";
                                    }
                                    if (!videoCodecBadge.Equals("")) {
                                        if (!badgeString.Equals("")) {
                                            badgeString += "%2C";
                                        }
                                        badgeString += videoCodecBadge;
                                        addedVideoCodec = true;
                                    }
                                }
                            }
                        } else if (stream.Type.Equals(MediaStreamType.Audio)) {

                            if (audioChannels.Equals("1") && !addedAudioChannels) {
                                if (stream.Channels.HasValue)
                                {
                                    var audBadge = "";
                                    if (stream.Channels.Equals(2)) {
                                        audBadge = "audio20";
                                    }
                                    else if (stream.Channels.Equals(6)) {
                                        audBadge = "audio51";
                                    }
                                    else if (stream.Channels.Equals(8)) {
                                        audBadge = "audio71";
                                    }
                                    if (!audBadge.Equals("")) {
                                        if (!badgeString.Equals("")) {
                                            badgeString += "%2C";
                                        }
                                        badgeString += audBadge;
                                        addedAudioChannels = true;
                                    }
                                }
                            }

                        }

                    }

                    if (!badgeString.Equals("")) {
                        fallback += "&badges=";
                        fallback += badgeString;
                        var badgeSize = RpdbSeriesProvider.Current.GetRpdbOptions().BadgeSize;
                        if (badgeSize.Equals("small")) {
                            fallback += "&badgeSize=small";
                        }
                        var badgePos = RpdbSeriesProvider.Current.GetRpdbOptions().BadgePos;
                        if (badgePos.Equals("center")) {
                            fallback += "&badgePos=center";
                        } else if (badgePos.Equals("right")) {
                            fallback += "&badgePos=right";
                        }
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