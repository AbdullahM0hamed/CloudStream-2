using CloudStreamForms.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace CloudStreamForms.Models
{
    public class EpisodeResult : ICloneable
    {
        public int Id { set; get; }
        public string IMDBEpisodeId;
        public int Episode { set; get; } = -1;
        public int Season { set; get; } = -1;
        public string Title { set; get; }
        public string Rating { set; get; }
        public string RatingStar { get { return (Rating.Replace(" ", "") == "" ? "Rating Unavailable" : "★ " + Rating); } } // ★
        public string PosterUrl { set; get; }
        public bool IsPosterFromStorage { get { return PosterUrl == CloudStreamCore.VIDEO_IMDB_IMAGE_NOT_FOUND; } }
        public ImageSource ImageSource { get { return IsPosterFromStorage ? App.GetImageSource(PosterUrl) : PosterUrl; } }
        public ImageSource VideoSource { get { return App.GetImageSource("nexflixPlayBtt.png"); } }
        public ImageSource DownloadSource {
            get {
                if (IsDownloaded) {
                    return "NetflixDownload3v2.png";//"NetflixDownload2.png";
                }
                else if (IsDownloading) {
                    return "CancelCrossV2.png";//"netflixStop128v2.png";
                }
                return "NetflixDownload1.png";
            }
        }

        public double DownloadSize {
            get {
                if (IsDownloaded) {
                    return 0.8;
                }
                else if (IsDownloading) {
                    return 0.8;
                }
                return 0.8;
            }
        }

        public bool ForceDescript = false;
        public ImageSource DownloadPlayBttSource { get; set; }
        public Command TapCom { set; get; }
        public Command TapComTwo { set; get; }
        public bool IsDownloading { get { return downloadState == 2; } }
        public bool IsDownloaded { get { return downloadState == 1; } }
        public bool IsSearchingDownloadResults { get { return downloadState == 4; } }
        public bool IsNotSearchingDownloadResults { get { return !IsSearchingDownloadResults; } }

        /// <summary>
        /// 0 = Nothing, 1 = Downloaded, 2 = Downloading, 3 = Paused, 4 = Searching 
        /// </summary>
        public int downloadState { set; get; } = 0;
        public string extraInfo { set; get; }
        public string ExtraText { set; get; }
        public double ExtraProgress { set; get; }
        public bool DownloadNotDone { set; get; }

        string _Description = "";
        string _ExtraDescription = "";
        public string Description { set { _Description = (Settings.EpDecEnabled || ForceDescript) ? value : ""; } get { return _Description; } }
        public string ExtraDescription { set { _ExtraDescription = (Settings.EpDecEnabled || ForceDescript) ? value : ""; } get { return _ExtraDescription; } }
        public double Progress { set; get; }
        public long ProgressState = -2;
        public bool HasProgress { get { return Progress > 0.05; } } // OVER 5% SO YOU CAN REMOVE IT
        /*    public List<string> Mirros { set; get; }
            public List<string> mirrosUrls { set; get; }
            public List<string> subtitles { set; get; }
            public List<string> subtitlesUrls { set; get; }*/
        public bool epVis { set; get; }
        // public LoadResult loadResult { set; get; }
        public bool GetHasLoadedLinks()
        {
            return GetMirrosUrls().Count > 0;
        }

        public List<string> GetMirrosUrls()
        {
            return GetBasicLinks().Where(t => !t.isAdvancedLink).Select(t => t.baseUrl).ToList();
        }


        public List<CloudStreamCore.BasicLink> GetBasicLinks()
        {
            var val = CloudStreamCore.GetCachedLink(IMDBEpisodeId);
            if (val == null) return new List<CloudStreamCore.BasicLink>();
            if (val.Value.links.Count == 0) return new List<CloudStreamCore.BasicLink>();
            return val.Value.links.OrderBy(t => -t.priority).ToList();
        }

        public List<string> GetMirros()
        {
            return GetBasicLinks().Where(t => !t.isAdvancedLink).Select(t => t.PublicName).ToList();
        }

        public void ClearMirror()
        {
            CloudStreamCore.ClearCachedLink(IMDBEpisodeId);
        }

        public string MainTextColor { set; get; } = "#e70000";
        public string MainDarkTextColor { get; set; } = "#a4a4a4"; //"#808080";
        public string ExtraColor { get; set; } = "#a4a4a4"; //"#808080";
        public string OgTitle { set; get; }

        public string GetDownloadTitle(int season, int episode)
        {
            if (season == -1 || episode == -1) {
                return OgTitle;
            }
            else {
                return $"S{season}:E{episode} {OgTitle}";
            }
        }

        public double TranslateYOffset {
            get {
                if (Device.RuntimePlatform == Device.UWP) {
                    return -20;
                }
                else {
                    return 0;
                }
            }
        }
        public double TranslateYOffsetVertical {
            get {
                if (Device.RuntimePlatform == Device.UWP) {
                    return 0;
                }
                else {
                    return 0;
                }
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
    /*
    public enum LoadSelection { Play,Download,CopyLink,CopySubtitleLink }

    public struct LoadResult
    {
        public string url;
        public string subtitleUrl;
        public LoadSelection loadSelection;
    }*/
}
