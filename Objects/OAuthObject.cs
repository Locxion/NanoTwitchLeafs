namespace NanoTwitchLeafs.Objects
{
    /// <summary>
    /// OAuth token container for Twitch authentication.
    /// </summary>
    public class OAuthObject
    {
        public string Access_Token { get; set; }
        public string Refresh_Token { get; set; }
        public int Expires_In { get; set; }
    }
}