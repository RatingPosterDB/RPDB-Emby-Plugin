namespace RPDB
{
    public class RpdbOptions
    {
        /// <summary>
        /// Gets or sets the user API key.
        /// </summary>
        /// <value>The user API key.</value>
        public string UserApiKey { get; set; }
        public string PosterType { get; set; }
        public string PosterLang { get; set; }
        public string Textless { get; set; }
        public string Backdrops { get; set; }
        public string FirstRating { get; set; }
        public string SecondRating { get; set; }
        public string ThirdRating { get; set; }
        public string FirstBackupRating { get; set; }
        public string SecondBackupRating { get; set; }
        public string Video3D { get; set; }
        public string VideoQuality { get; set; }
        public string ColorRange { get; set; }
        public string AudioChannels { get; set; }
        public string VideoCodec { get; set; }
        public string BadgeSize { get; set; }
        public string BadgePos { get; set; }
    }
}