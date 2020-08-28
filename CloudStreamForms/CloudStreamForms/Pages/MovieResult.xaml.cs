using CloudStreamForms.Core;
using CloudStreamForms.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.App;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.MainPage;
using static CloudStreamForms.Settings;
using static CloudStreamForms.Core.CoreHelpers;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MovieResult : ContentPage
    {
        const uint FATE_TIME_MS = 500;

        CloudStreamCore core;

        public int SmallFontSize { get; set; } = 11;
        public int WithSize { get; set; } = 50;
        List<Episode> CurrentEpisodes { get { return currentMovie.episodes; } }

        public MovieResultMainEpisodeView epView;

        readonly LabelList SeasonPicker;
        readonly LabelList DubPicker;
        LabelList FromToPicker;

        public Poster mainPoster;
        public string trailerUrl = "";
 
        //  public static List<Movie> lastMovie;
        List<Poster> RecomendedPosters { get { return currentMovie.title.recomended; } }  //= new List<Poster>();

        bool loadedTitle = false;
        int currentSeason = 0;
        //ListView episodeView;
        public const int heightRequestPerEpisode = 120;
        public const int heightRequestAddEpisode = 40;
        public const int heightRequestAddEpisodeAndroid = 0;

        bool isMovie = false;
        Movie currentMovie { get { return core.activeMovie; } }
        bool isDub = true;
       // bool RunningWindows { get { return Xamarin.Essentials.DeviceInfo.Platform == DevicePlatform.UWP; } }
        string CurrentMalLink {
            get {

                try {
                    string s = currentMovie.title.MALData.seasonData[currentSeason].malUrl;
                    if (s != "https://myanimelist.net") {
                        return s;
                    }
                    else {
                        return "";
                    }
                }
                catch (Exception) {
                    return "";
                }
            }
        }
        string CurrentAniListLink {
            get {

                try {
                    string s = currentMovie.title.MALData.seasonData[currentSeason].aniListUrl;
                    if (s.IsClean()) {
                        return s;
                    }
                    else {
                        return "";
                    }
                }
                catch (Exception) {
                    return "";
                }
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (ActionPopup.isOpen) return true;

            Search.mainPoster = new Poster();
            /*if (lastMovie != null) {
                if (lastMovie.Count > 1) {
                    core.activeMovie = lastMovie[lastMovie.Count - 1];
                    lastMovie.RemoveAt(lastMovie.Count - 1);
                }
            }*/
            if (setKey) {
                App.RemoveKey(App.BOOKMARK_DATA, currentMovie.title.id);
            }
            Dispose();
            return base.OnBackButtonPressed();

            //     Navigation.PopModalAsync(false);
            //     return true;
        }

        bool setKey = false;
        void SetKey()
        {
            App.SetKey(App.BOOKMARK_DATA, currentMovie.title.id, "Name=" + currentMovie.title.name + "|||PosterUrl=" + currentMovie.title.hdPosterUrl + "|||Id=" + currentMovie.title.id + "|||TypeId=" + ((int)currentMovie.title.movieType) + "|||ShortEpView=" + currentMovie.title.shortEpView + "|||=EndAll");
            setKey = false;
        }

        private void StarBttClicked(object sender, EventArgs e)
        {
            Home.UpdateIsRequired = true;

            bool keyExists = App.KeyExists(App.BOOKMARK_DATA, currentMovie.title.id);
            if (keyExists) {
                App.RemoveKey(App.BOOKMARK_DATA, currentMovie.title.id);
            }
            else {
                if (currentMovie.title.name == null) {
                    App.SetKey(App.BOOKMARK_DATA, currentMovie.title.id, "Name=" + currentMovie.title.name + "|||Id=" + currentMovie.title.id + "|||");

                    setKey = true;
                }
                else {
                    SetKey();
                }
            }
            ChangeStar(!keyExists);
        }

        private void SubtitleBttClicked(object sender, EventArgs e)
        {
            Settings.SubtitlesEnabled = !Settings.SubtitlesEnabled;
            ChangeSubtitle(SubtitlesEnabled);
        }

        private void ShareBttClicked(object sender, EventArgs e)
        {
            if (currentMovie.title.id != "" && currentMovie.title.name != "") {
                Share();
            }
        }

        async void Share()
        {
            List<string> actions = new List<string>() { "Everything", "CloudStream Link", "IMDb Link", "Title", "Title and Description" };
            if (CurrentMalLink != "") {
                actions.Insert(3, "MAL Link");
            }
            if (CurrentAniListLink.IsClean()) {
                actions.Insert(4, "AniList Link");
            }
            if (trailerUrl != "") {
                actions.Insert(actions.Count - 2, "Trailer Link");
            }
            string a = await ActionPopup.DisplayActionSheet("Copy", actions.ToArray());//await DisplayActionSheet("Copy", "Cancel", null, actions.ToArray());
            string copyTxt = "";

            async Task<string> GetMovieCode()
            {
                await ActionPopup.StartIndeterminateLoadinbar("Loading...");
                bool done = false;
                string _s = "";
                Thread t = new Thread(() => {
                    try {
                        _s = CloudStreamCore.ShareMovieCode(currentMovie.title.id + "Name=" + currentMovie.title.name + "=EndAll");
                    }
                    catch (System.Exception) {
                    }
                    finally {
                        done = true;
                    }
                });
                t.Start();
                while (!done) {
                    await Task.Delay(10);
                }
                await ActionPopup.StopIndeterminateLoadinbar();
                return _s;
            }

            if (a == "CloudStream Link") {
                string _s = await GetMovieCode();
                if (_s != "") {
                    copyTxt = _s;
                }
            }
            else if (a == "IMDb Link") {
                copyTxt = "https://www.imdb.com/title/" + currentMovie.title.id;
            }
            else if (a == "Title") {
                copyTxt = currentMovie.title.name;
            }
            else if (a == "MAL Link") {
                copyTxt = CurrentMalLink;
            }
            else if (a == "Title and Description") {
                copyTxt = currentMovie.title.name + "\n" + currentMovie.title.description;
            }
            else if (a == "Trailer Link") {
                copyTxt = trailerUrl;
            }
            else if (a == "AniList Link") {
                copyTxt = CurrentAniListLink;
            }
            else if (a == "Everything") {
                copyTxt = currentMovie.title.name + " | " + RatingLabel.Text + "\n" + currentMovie.title.description;
                string _s = await GetMovieCode();

                if (_s != "") {
                    copyTxt = copyTxt + "\nCloudStream: " + _s;
                }
                copyTxt = copyTxt + "\nIMDb: " + "https://www.imdb.com/title/" + currentMovie.title.id;
                if (CurrentMalLink != "") {
                    copyTxt += "\nMAL: " + CurrentMalLink;
                }
                if (CurrentAniListLink.IsClean()) {
                    copyTxt += "\nAniList: " + CurrentAniListLink;
                }
                if (trailerUrl != "") {
                    copyTxt += "\nTrailer: " + trailerUrl;
                }
            }
            if (a != "Cancel" && copyTxt != "") {
                await Clipboard.SetTextAsync(copyTxt);
                App.ShowToast("Copied " + a + " to Clipboard");
            }

        }

        void ChangeStar(bool? overrideBool = null, string key = null)
        {
            if (core == null) return;

            bool keyExists = false;
            if (key == null) {
                key = currentMovie.title.id;
            }
            if (overrideBool == null) {
                keyExists = App.KeyExists(App.BOOKMARK_DATA, key);
                print("KEYEXISTS:" + keyExists + "|" + currentMovie.title.id);
            }
            else {
                keyExists = (bool)overrideBool;
            }
            StarBtt.Source = keyExists ? "bookmark.svg" : "bookmark_off.svg";//GetImageSource(());
            Device.BeginInvokeOnMainThread(() => {
                StarBtt.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(keyExists ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
            });
        }

        void ChangeSubtitle(bool? overrideBool = null)
        {
            if (core == null) return;

            bool res = false;
            if (overrideBool == null) {

                res = SubtitlesEnabled;
            }
            else {
                res = (bool)overrideBool;
                //SubtitlesEnabled = res;
            }

            Device.BeginInvokeOnMainThread(() => {
                SubtitleBtt.Source = res ? "subtitles.svg" : "subtitles_off.svg";//App.GetImageSource();
                SubtitleBtt.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(res ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
            });
        }

        public void SetChromeCast(bool enabled)
        {
            if (core == null) return;

            ChromeCastBtt.IsVisible = enabled;
            ChromeCastBtt.IsEnabled = enabled;
            ImgChromeCastBtt.IsVisible = enabled;
            ImgChromeCastBtt.IsEnabled = enabled;
            if (enabled) {
                ImgChromeCastBtt.Source = GetImageSource(MainChrome.CurrentImageSource);
            }
            NameLabel.Margin = new Thickness((enabled ? 50 : 15), 10, 10, 10);
        }


        bool hasAppeared = false;
        protected override void OnAppearing()
        {
            base.OnAppearing();

            ForceUpdate(checkColor: true);
            OnSomeDownloadFinished += OnHandleDownload;
            OnSomeDownloadFailed += OnHandleDownload;

            if (!hasAppeared) {
                hasAppeared = true;
                ForceUpdateVideo += ForceUpdateAppearing;
            }

            print("APPEARING;::");
            //FadeAppear();
            SetChromeCast(MainChrome.IsChromeDevicesOnNetwork);

        }

        public void OnHandleDownload(object sender, EventArgs arg)
        {
            ForceUpdate(checkColor: true);
        }

        protected override void OnDisappearing()
        {
            OnSomeDownloadFinished -= OnHandleDownload;
            OnSomeDownloadFailed -= OnHandleDownload;

            //  ForceUpdateVideo -= ForceUpdateAppearing; // CANT REMOVE IT BECAUSE VIDEOPAGE TRIGGERS ONDIS
            base.OnDisappearing();
        }

        public void ForceUpdateAppearing(object s, EventArgs e)
        {
            ForceUpdate();
        }

        private void ChromeCastBtt_Clicked(object sender, EventArgs e)
        {
            WaitChangeChromeCast();
        }

        public static void OpenChrome(bool validate = true)
        {
            if (!ChromeCastPage.isActive) {

                Page p = ChromeCastPage.CreateChromePage(chromeResult, chromeMovieResult);// new (chromeResult, chromeMovieResult); //{ episodeResult = chromeResult, chromeMovieResult = chromeMovieResult };
                MainPage.mainPage.Navigation.PushModalAsync(p, false);
            }
        }

        private void OpenChromecastView(object sender, EventArgs e)
        {
            if (core == null) return;

            if (sender != null) {
                ChromeCastPage.isActive = false;
            }
            if (!ChromeCastPage.isActive) {
                OpenChrome(false);
                //      Page p = new ChromeCastPage() { episodeResult = chromeResult, chromeMovieResult = chromeMovieResult };
                //    Navigation.PushModalAsync(p, false);
            }
        }

        public static EpisodeResult chromeResult;
        public static Movie chromeMovieResult;
        async void WaitChangeChromeCast()
        {
            if (core == null) return;

            if (MainChrome.IsCastingVideo) {
                Device.BeginInvokeOnMainThread(() => {
                    OpenChromecastView(1, EventArgs.Empty);
                });
            }
            else {
                List<string> names = MainChrome.GetChromeDevicesNames();
                if (MainChrome.IsConnectedToChromeDevice) { names.Add("Disconnect"); }
                string a = await ActionPopup.DisplayActionSheet("Cast to", names.ToArray());//await DisplayActionSheet("Cast to", "Cancel", MainChrome.IsConnectedToChromeDevice ? "Disconnect" : null, names.ToArray());
                if (a != "Cancel") {
                    MainChrome.ConnectToChromeDevice(a);
                }
            }
        }

        public static ImageSource GetGradient()
        {
            return GetImageSource("gradient" + Settings.BlackColor + ".png");//BlackBg ? "gradient.png" : "gradientGray.png");
        }

        /*
        async void FadeAppear()
        {
            NormalStack.Opacity = 0;
            NormalStack.Scale = 0.7;
            await Task.Delay(100);
            NormalStack.FadeTo(1);
            NormalStack.ScaleTo(1);
        }*/

        public MovieResult()
        {
            InitializeComponent();
            core = new CloudStreamCore();

            mainPoster = Search.mainPoster;

            Gradient.Source = GetGradient();
            Gradient.HeightRequest = 200;

            //   IMDbBtt.Source = GetImageSource("IMDbWhite.png");//"imdbIcon.png");
            //   MALBtt.Source = GetImageSource("MALWhite.png");//"MALIcon.png");
            //   ShareBtt.Source = GetImageSource("baseline_share_white_48dp.png");//GetImageSource("round_reply_white_48dp_inverted.png");
            //  StarBtt.Source = GetImageSource("notBookmarkedBtt.png");
            //   SubtitleBtt.Source = GetImageSource("outline_subtitles_white_48dp.png"); //GetImageSource("round_subtitles_white_48dp.png");
            //  IMDbBlue.Source = GetImageSource("IMDbBlue.png"); //GetImageSource("round_subtitles_white_48dp.png");

            ReviewLabel.Clicked += (o, e) => {
                if (!ReviewPage.isOpen) {
                    Page _p = new ReviewPage(currentMovie.title.id, mainPoster.name);
                    MainPage.mainPage.Navigation.PushModalAsync(_p);
                }
            };

            SeasonPicker = new LabelList(SeasonBtt, new List<string>());
            DubPicker = new LabelList(DubBtt, new List<string>());
            // FromToPicker = new LabelList(FromToBtt, new List<string>());

            // -------------- CHROMECASTING THINGS --------------

            if (Device.RuntimePlatform == Device.UWP) {
                ImgChromeCastBtt.TranslationX = 0;
                ImgChromeCastBtt.TranslationY = 0;
                OffBar.IsVisible = false;
                OffBar.IsEnabled = false;
            }
            else {
                OffBar.Source = App.GetImageSource("gradient.png"); OffBar.HeightRequest = 3; OffBar.HorizontalOptions = LayoutOptions.Fill; OffBar.ScaleX = 100; OffBar.Opacity = 0.3; OffBar.TranslationY = 9;
            }

            MainChrome.OnChromeImageChanged += (o, e) => {
                ImgChromeCastBtt.Source = GetImageSource(e);
                ImgChromeCastBtt.Transformations.Clear();
                if (MainChrome.IsConnectedToChromeDevice) {
                    // ImgChromeCastBtt.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation("#303F9F")) };
                }
            };

            MainChrome.OnChromeDevicesFound += (o, e) => {
                SetChromeCast(MainChrome.IsChromeDevicesOnNetwork);
            };

            MainChrome.OnVideoCastingChanged += (o, e) => {
                if (e) {
                    OpenChromecastView(null, null);
                }
            };

            if (!MainChrome.IsConnectedToChromeDevice) {
                MainChrome.GetAllChromeDevices();
            }

            Recommendations.SizeChanged += (o, e) => {
                SetRecs();
            };


            //ViewToggle.Source = GetImageSource("viewOnState.png");
            ChangeViewToggle();
            ChangeSubtitle();

            //NameLabel.Text = activeMovie.title.name;
            NameLabel.Text = mainPoster.name;
            RatingLabel.Text = mainPoster.year;

            core.titleLoaded += MovieResult_titleLoaded;
            core.trailerLoaded += MovieResult_trailerLoaded;
            core.episodeLoaded += EpisodesLoaded;
            core.episodeHalfLoaded += EpisodesHalfLoaded;


            // TrailerBtt.Clicked += TrailerBtt_Clicked;
            TrailerBtt.Clicked += TrailerBtt_Clicked;

            BatchDownloadBtt.Clicked += async (o, e) => {
                var episodes = epView.MyEpisodeResultCollection.Select(t => (EpisodeResult)t.Clone()).ToArray();
                CloudStreamCore coreCopy = (CloudStreamCore)core.Clone();
                int _currentSeason = currentSeason;
                bool _isDub = isDub;

                int max = episodes.Length;
                List<string> res = await ActionPopup.DisplayLogin("Download", "Cancel", "Download Episodes", new LoginPopupPage.PopupFeildsDatas() { placeholder = "First episode (1)", res = InputPopupPage.InputPopupResult.integrerNumber }, new LoginPopupPage.PopupFeildsDatas() { placeholder = $"Last episode ({max})", res = InputPopupPage.InputPopupResult.integrerNumber });
                if (res.Count == 2) {
                    try {
                        int firstEp = res[0].IsClean() ? int.Parse(res[0]) : 1;
                        int lastEp = res[1].IsClean() ? int.Parse(res[1]) : max;
                        if (lastEp <= 0 || firstEp <= 0) {
                            return;
                        }
                        lastEp = Math.Clamp(lastEp, 1, max);
                        firstEp = Math.Clamp(firstEp, 1, lastEp);

                        App.ShowToast($"Downloading Episodes {firstEp}-{lastEp}");
                        for (int i = firstEp - 1; i < lastEp; i++) {  
                            var ep = episodes[i];
                            string imdbId = ep.IMDBEpisodeId;
                            CloudStreamCore.Title titleName = (Title)coreCopy.activeMovie.title.Clone();

                            coreCopy.GetEpisodeLink(coreCopy.activeMovie.title.IsMovie ? -1 : (ep.Id + 1), _currentSeason, false, false, _isDub);

                            int epId = GetCorrectId(ep,coreCopy.activeMovie);
                            await Task.Delay(10000); // WAIT 10 Sec
                            try {
                                BasicLink[] info = null;
                                bool hasMirrors = false;
                                var baseLinks = CloudStreamCore.GetCachedLink(imdbId);
                                if (baseLinks.HasValue) {
                                    info = baseLinks.Value.links.Where(t => t.CanBeDownloaded).ToList().OrderHDLinks().ToArray();
                                    hasMirrors = info.Length > 0;
                                }

                                if (hasMirrors && info != null) {
                                    App.UpdateDownload(epId, -1);
                                    string dpath = App.RequestDownload(epId, ep.OgTitle, ep.Description, ep.Episode, currentSeason, info.Select(t => { return new BasicMirrorInfo() { mirror = t.baseUrl, name = t.PublicName, referer = t.referer }; }).ToList(), ep.GetDownloadTitle(_currentSeason, ep.Episode) + ".mp4", ep.PosterUrl, titleName, ep.IMDBEpisodeId);
                                }
                                else {
                                    App.ShowToast("Download Failed, No Mirrors Found");
                                }
                            }
                            catch (Exception _ex) {
                                print("EX DLOAD::: DOWNLOAD:::: " + _ex);
                            }
                        }
                    }
                    catch (Exception _ex) {
                        App.ShowToast("Error batch downloading episodes");
                    }
                }
            };
            //  core.linkAdded += MovieResult_linkAdded;


            core.moeDone += MovieResult_moeDone;

            BackgroundColor = Settings.BlackRBGColor;
            BgColorSet.BackgroundColor = Settings.BlackRBGColor;

            /*
            MALB.IsVisible = false;
            MALB.IsEnabled = false;
            MALA.IsVisible = false;
            MALA.IsEnabled = false;
            Grid.SetColumn(MALB, 0);
            Grid.SetColumn(MALA, 0);*/

            DubBtt.IsVisible = false;
            SeasonBtt.IsVisible = false;

            epView = new MovieResultMainEpisodeView();
            SetHeight();

            BindingContext = epView;
            episodeView.VerticalScrollBarVisibility = Settings.ScrollBarVisibility;
            //  RecStack.HorizontalScrollBarVisibility = Settings.ScrollBarVisibility; // REPLACE REC

            ReloadAllBtt.Clicked += (o, e) => {
                App.RemoveKey("CacheImdb", currentMovie.title.id);
                App.RemoveKey("CacheMAL", currentMovie.title.id);
                Search.mainPoster = new Poster();
                Navigation.PopModalAsync(false);
                PushPageFromUrlAndName(currentMovie.title.id, mainPoster.name);
                Dispose();

            };
            ReloadAllBtt.Source = GetImageSource("round_refresh_white_48dp.png");

            core.GetImdbTitle(mainPoster);
            ChangeStar();

            ChangedRecState(0, true);


            /*
            Commands.SetTap(NotificationBtt, new Command(() => {
                ToggleNotify();
            }));
            */

            SkipAnimeBtt.Clicked += (o, e) => {
                // Grid.SetColumn(SkipAnimeBtt, 0);
                Device.BeginInvokeOnMainThread(() => {
                    shouldSkipAnimeLoading = true;
                    SkipAnimeBtt.IsVisible = false;
                    SkipAnimeBtt.IsEnabled = false;
                    hasSkipedLoading = true;
                });
            };

            fishProgressLoaded += (o, e) => {
                if (core == null) return;

                Device.BeginInvokeOnMainThread(() => {
                    SkipAnimeBtt.Text = $"Skip - {e.currentProgress} of {e.maxProgress}"; // {(int)(e.progressProcentage * 100)}%

                    if (e.progressProcentage > 0) {
                        if (!SkipAnimeBtt.IsVisible && !hasSkipedLoading) {
                            SkipAnimeBtt.Opacity = 0;
                            // Grid.SetColumn(SkipAnimeBtt, 1);
                            SkipAnimeBtt.IsVisible = true;
                            SkipAnimeBtt.IsEnabled = true;
                            SkipAnimeBtt.FadeTo(1);
                        }
                    }
                    if (e.progressProcentage >= 1) {
                        hasSkipedLoading = true;
                        //  Grid.SetColumn(SkipAnimeBtt, 0);
                        SkipAnimeBtt.IsVisible = false;
                        SkipAnimeBtt.IsEnabled = false;
                    }

                    /*
                   if (e.progress >= 1 && (!FishProgress.IsVisible || FishProgress.Progress >= 1)) return;
                   FishProgress.IsVisible = true;
                   FishProgress.IsEnabled = true;
                   FishProgressTxt.IsVisible = true;
                   FishProgressTxt.IsEnabled = true;
                   if (FishProgress.Opacity == 0) {
                       FishProgress.FadeTo(1);
                   }

                   // FishProgressTxt.Text = e.name;
                   await FishProgress.ProgressTo(e.progress, 250, Easing.SinIn);
                   if (e.progress >= 1) {

                       FishProgressTxt.IsVisible = false;
                       FishProgressTxt.IsEnabled = false;

                       await FishProgress.FadeTo(0);
                       FishProgress.IsVisible = false;
                       FishProgress.IsEnabled = false;
                   }*/
                });

            };

        }


        bool hasSkipedLoading = false;

        // NOTIFICATIONS
        /*
        void CancelNotifications()
        {
            if (core == null) return;

            var keys = App.GetKey<List<int>>("NotificationsIds", currentMovie.title.id, new List<int>());
            for (int i = 0; i < keys.Count; i++) {
                App.CancelNotifaction(keys[i]);
            }
        }

        void AddNotifications()
        {
            if (core == null) return;

            List<int> keys = new List<int>();

            for (int i = 0; i < setNotificationsTimes.Count; i++) {
                // GENERATE UNIQUE ID
                int _id = 1337 + setNotificationsTimes[i].number * 100000000 + int.Parse(currentMovie.title.id.Replace("tt", ""));// int.Parse(setNotificationsTimes[i].number + currentMovie.title.id.Replace("tt", ""));
                keys.Add(_id);
                print("BIGICON:::" + currentMovie.title.hdPosterUrl + "|" + currentMovie.title.posterUrl);//setNotificationsTimes[i].timeOfRelease);//
                App.ShowNotIntent("NEW EPISODE - " + currentMovie.title.name, setNotificationsTimes[i].episodeName, _id, currentMovie.title.id, currentMovie.title.name, bigIconUrl: currentMovie.title.hdPosterUrl, time: setNotificationsTimes[i].timeOfRelease);// DateTime.UtcNow.AddSeconds(10));//ShowNotification("NEW EPISODE - " + currentMovie.title.name, setNotificationsTimes[i].episodeName, _id, i * 10);
            }
            App.SetKey("NotificationsIds", currentMovie.title.id, keys);
        }

        void ToggleNotify()
        {
            if (core == null) return;

            bool hasNot = App.GetKey<bool>("Notifications", currentMovie.title.id, false);
            App.SetKey("Notifications", currentMovie.title.id, !hasNot);
            UpdateNotification(!hasNot);

            if (!hasNot) {
                AddNotifications();
            }
            else {
                CancelNotifications();
            }
        }*/
        /*
        void UpdateNotification(bool? overrideNot = null)
        {
            if (core == null) return;
            if (!FETCH_NOTIFICATION) return;
            
            bool hasNot = overrideNot ?? App.GetKey<bool>("Notifications", currentMovie.title.id, false);
            NotificationImg.Source = hasNot ? "notifications_active.svg" : "notifications.svg"; //App.GetImageSource(hasNot ? "baseline_notifications_active_white_48dp.png" : "baseline_notifications_none_white_48dp.png");
            NotificationImg.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(hasNot ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
            NotificationTime.TextColor = hasNot ? Color.FromHex(DARK_BLUE_COLOR) : Color.Gray;
    }*/

        // List<MoeEpisode> setNotificationsTimes = new List<MoeEpisode>();

        private void MovieResult_moeDone(object sender, List<MoeEpisode> e)
        {
            /*
            if (core == null) return;
            if (!FETCH_NOTIFICATION) return;
            if (e == null) return;
            print("MOE DONE:::: + " + e.Count);
            for (int i = 0; i < e.Count; i++) {
                print("MOE______ " + e[i].episodeName);
            }
            void FadeIn()
            {
                NotificationTime.Opacity = 0;
                NotificationTime.FadeTo(1, FATE_TIME_MS);
            }

            if (e.Count <= 0) {
                Device.BeginInvokeOnMainThread(() => {
                    NotificationTime.Text = "Completed";
                    FadeIn();
                });
                return;
            };

            setNotificationsTimes = e;

            Device.BeginInvokeOnMainThread(() => {
                try {
                    AddNotifications(); // UPDATE NOTIFICATIONS
                }
                catch (Exception _ex) {
                    print("NOTIFICATIONS ADD ERROR: " + _ex);
                }
                try {
                    NotificationTime.Text = "Completed";
                    FadeIn();
                    for (int i = e.Count - 1; i >= 0; i--) {
                        var diff = e[i].DiffTime;
                        print("DIFFTIME:::::" + e[i].DiffTime);
                        if (diff.TotalSeconds > 0) {
                            NotificationTime.Text = "Next Epiode: " + (diff.Days == 0 ? "" : (diff.Days + "d ")) + (diff.Hours == 0 ? "" : (diff.Hours + "h ")) + diff.Minutes + "m";
                            UpdateNotification();
                            NotificationBtt.IsEnabled = true;
                            return;
                        }
                    }
                }
                catch (Exception _ex) {
                    print("EXKKK::" + _ex);
                }
            });*/
        }


        public void SetColor(EpisodeResult episodeResult)
        {
            if (core == null) return;

            string id = episodeResult.IMDBEpisodeId;
            if (id != "") {
                List<string> hexColors = new List<string>() { "#ffffff", LIGHT_BLUE_COLOR, "#e5e598" };
                List<string> darkHexColors = new List<string>() { "#900000", DARK_BLUE_COLOR, "#d3c450" };
                int color = 0;
                if (App.KeyExists(App.VIEW_HISTORY, id)) {
                    color = 1;
                }

                DownloadState state = App.GetDstate(GetCorrectId(episodeResult));
                switch (state) {
                    case DownloadState.Downloading:
                        episodeResult.downloadState = 2;
                        break;
                    case DownloadState.Downloaded:
                        episodeResult.downloadState = 1;
                        break;
                    case DownloadState.NotDownloaded:
                        episodeResult.downloadState = 0;
                        break;
                    case DownloadState.Paused:
                        episodeResult.downloadState = 3;
                        break;
                    default:
                        break;
                }
                print("SETCOLOR::: " + state);

                /*
                if (App.KeyExists("dlength", "id" + GetCorrectId(episodeResult))) {
                    try {
                        DownloadState state = App.GetDownloadInfo(GetCorrectId(episodeResult)).state.state;
                        switch (state) {
                            case DownloadState.Downloading:
                                episodeResult.downloadState = 2;
                                break;
                            case DownloadState.Downloaded:
                                episodeResult.downloadState = 1;
                                break;
                            case DownloadState.NotDownloaded:
                                episodeResult.downloadState = 1;
                                break;
                            case DownloadState.Paused:
                                episodeResult.downloadState = 3;
                                break;
                            default:
                                break;
                        }
                        print("STATE:::: " + episodeResult.downloadState + "|" + state);
                      //  color = 2;
                    }
                    catch (Exception _ex) {
                        print("EX:::" + _ex);
                    }

                } else {
                    episodeResult.downloadState = 0;
                }*/

                episodeResult.MainTextColor = hexColors[color];
                episodeResult.MainDarkTextColor = darkHexColors[color];
            }
        }


        async void SetEpisodeFromTo(int segment, int max = -1)
        {
            if (core == null) return;

            epView.MyEpisodeResultCollection.Clear();

            int start = MovieResultMainEpisodeView.MAX_EPS_PER * segment;
            if (max == -1) {
                max = epView.AllEpisodes.Length;
            }
            else {
                max = Math.Min(max, epView.AllEpisodes.Length);
            }

            int end = Math.Min(MovieResultMainEpisodeView.MAX_EPS_PER * (segment + 1), max);
            SetHeight(null, end - start);
            RecomendationLoaded.IsVisible = false;

            FadeEpisodes.Opacity = 0;
            for (int i = start; i < end; i++) {
                // await Task.Delay(30);
                epView.MyEpisodeResultCollection.Add(epView.AllEpisodes[i]);
            }
            await Task.Delay(100);
            await FadeEpisodes.FadeTo(1);

        }

        int maxEpisodes = 1;
        public void AddEpisode(EpisodeResult episodeResult, int index)
        {
            if (core == null) return;

            var _episode = ChangeEpisode(episodeResult);
            epView.AllEpisodes[index] = _episode;
        }

        EpisodeResult UpdateLoad(EpisodeResult episodeResult, bool checkColor = false)
        {
            if (core == null) return null;
            long pos;
            long len;
            if (checkColor) {
                SetColor(episodeResult);
            }
            print("POST PRO ON: " + episodeResult.IMDBEpisodeId);
            string realId = episodeResult.IMDBEpisodeId;
            print("ID::::::: ON " + realId + "|" + App.GetKey(VIEW_TIME_POS, realId, -1L));
            if ((pos = App.GetViewPos(realId)) > 0) {
                if ((len = App.GetViewDur(realId)) > 0) {
                    episodeResult.Progress = (double)pos / (double)len;
                    episodeResult.ProgressState = pos;
                    print("MAIN DRI:: " + pos + "|" + len + "|" + episodeResult.Progress);
                }//tt8993804 // tt0772328
            }
            return episodeResult;
        }

        static bool canTapEpisode = true;
        EpisodeResult ChangeEpisode(EpisodeResult episodeResult)
        {
            episodeResult.OgTitle = episodeResult.Title;
            SetColor(episodeResult);
            episodeResult.Season = currentSeason;

            /*if (episodeResult.Rating != "") {
                episodeResult.Title += " | ★ " + episodeResult.Rating;
            }*/
            if (episodeResult.Rating == "") {
                episodeResult.Rating = currentMovie.title.rating;
            }

            if (!isMovie) {
                episodeResult.Title = episodeResult.Episode + ". " + episodeResult.Title;
                print("ADDMOVIE:___" + episodeResult.Episode + "|" + episodeResult.Title);
            }

            if (episodeResult.PosterUrl == "") {
                if (core.activeMovie.title.posterUrl != "") {
                    string posterUrl = "";
                    try {
                        if (core.activeMovie.title.trailers.Count > 0) {
                            if (core.activeMovie.title.trailers[0].PosterUrl != null) {
                                posterUrl = core.activeMovie.title.trailers[0].PosterUrl;
                            }
                        }
                    }
                    catch (Exception) {

                    }
                    episodeResult.PosterUrl = posterUrl;
                }
            }
            if (episodeResult.PosterUrl == "") {
                episodeResult.PosterUrl = CloudStreamCore.VIDEO_IMDB_IMAGE_NOT_FOUND;
            }
            else {
                episodeResult.PosterUrl = CloudStreamCore.ConvertIMDbImagesToHD(episodeResult.PosterUrl, 224, 126); //episodeResult.PosterUrl.Replace(",126,224_AL", "," + pwidth + "," + pheight + "_AL").Replace("UY126", "UY" + pheight).Replace("UX224", "UX" + pwidth);
            }
            episodeResult.Progress = 0;

            UpdateLoad(episodeResult);

            int GetRealIdFromId()
            {
                for (int i = 0; i < epView.MyEpisodeResultCollection.Count; i++) {
                    if (epView.MyEpisodeResultCollection[i].Id == episodeResult.Id) {
                        return i;
                    }
                }
                return -1;
            }

            episodeResult.TapComTwo = new Command(async (s) => {
                if (!canTapEpisode) return;
                if (core == null) return;

                int _id = GetRealIdFromId();
                if (_id == -1) return;
                canTapEpisode = false;
                try {
                    var epRes = epView.MyEpisodeResultCollection[_id];
                    if (epRes.downloadState == 1) {
                        PlayDownloadedEp(epRes);
                    }
                    else {
                        await LoadLinksForEpisode(epRes);
                    }
                }
                finally {
                    canTapEpisode = true;
                }
            });

            episodeResult.TapCom = new Command(async (s) => {
                if (core == null) return;

                int _id = GetRealIdFromId();
                if (_id == -1) return;

                var epRes = epView.MyEpisodeResultCollection[_id];
                if (epRes.downloadState == 4) return;

                void DeleteData()
                {
                    string downloadKeyData = App.GetDownloadInfo(GetCorrectId(epRes), false).info.fileUrl;//.GetKey("Download", GetId(episodeResult), "");
                    DeleteFile(downloadKeyData, epRes);
                }
                /*
                if (epRes.IsDownloading) { // REMOVE
                    bool action = await DisplayAlert("Delete file", "Do you want to delete " + epRes.OgTitle, "Delete", "Cancel");
                    if (action) {
                        DeleteData();
                    }
                }
                else*/
                if (epRes.IsDownloaded || epRes.IsDownloading) {
                    if (core == null) return;
                    string action = await ActionPopup.DisplayActionSheet(epRes.OgTitle, "Play", "Delete File"); //await DisplayActionSheet(epRes.OgTitle, "Cancel", null, "Play", "Delete File");
                    if (core == null) return;
                    if (action == "Delete File") {
                        DeleteData();
                    }
                    else if (action == "Play") {
                        PlayDownloadedEp(epRes);
                    }
                }
                else { // DOWNLOAD
                    if (core == null) return;
                    epView.MyEpisodeResultCollection[_id].downloadState = 4; // SET IS SEARCHING
                    ForceUpdate();
                    currentDownloadSearchesHappening++;
                    CloudStreamCore coreCopy = (CloudStreamCore)core.Clone();
                    string imdbId = episodeResult.IMDBEpisodeId;
                    CloudStreamCore.Title titleName = (Title)currentMovie.title.Clone();

                    coreCopy.GetEpisodeLink(isMovie ? -1 : (episodeResult.Id + 1), currentSeason, isDub: isDub, purgeCurrentLinkThread: currentDownloadSearchesHappening > 0);
                    print("!!___" + _id);
                    int epId = GetCorrectId(episodeResult);

                    var _episodeResult = (EpisodeResult)episodeResult.Clone();
                    await Task.Delay(10000); // WAIT 10 Sec
                    try {
                        // if (!SameAsActiveMovie()) return;

                        /*
                        Movie _currentMovie = coreCopy.activeMovie;


                        var l = 
                        for (int i = 0; i < l.Length; i++) {
                            print(l[i].name + "<<<<<<<LD");
                        }*/
                        BasicLink[] info = null;
                        bool hasMirrors = false;
                        var baseLinks = CloudStreamCore.GetCachedLink(imdbId);
                        if (baseLinks.HasValue) {
                            info = baseLinks.Value.links.Where(t => t.CanBeDownloaded).ToList().OrderHDLinks().ToArray();
                            hasMirrors = info.Length > 0;

                            /*.Where(t => {
                                return !t.isAdvancedLink;
                            });*/

                            /*.ToList();
                            hasMirrors = _links.Count > 0;

                            List<string> mirrorUrls = new List<string>();
                            List<string> mirrorNames = new List<string>();
                            foreach (var link in _links) {
                                mirrorNames.Add(link.PublicName);
                                mirrorUrls.Add(link.baseUrl);
                            }
                            info = SortToHdMirrors(epRes.mirrosUrls, epRes.Mirros);*/
                        }

                        if (hasMirrors && info != null) {
                            App.UpdateDownload(epId, -1);
                            string dpath = App.RequestDownload(epId, _episodeResult.OgTitle, _episodeResult.Description, _episodeResult.Episode, currentSeason, info.Select(t => { return new BasicMirrorInfo() { mirror = t.baseUrl, name = t.PublicName, referer = t.referer }; }).ToList(), _episodeResult.GetDownloadTitle(currentSeason, _episodeResult.Episode) + ".mp4", _episodeResult.PosterUrl, titleName, _episodeResult.IMDBEpisodeId);

                            try {
                                epView.MyEpisodeResultCollection[_id].downloadState = 2; // SET IS DOWNLOADING
                                ForceUpdate();
                            }
                            catch (Exception) { }
                        }
                        else {
                            App.ShowToast("Download Failed, No Mirrors Found");
                            try {
                                epView.MyEpisodeResultCollection[_id].downloadState = 0;
                                ForceUpdate();
                            }
                            catch (Exception) { }
                        }
                    }
                    catch (Exception _ex) {
                        print("EX DLOAD::: DOWNLOAD:::: " + _ex);
                    }
                    currentDownloadSearchesHappening--;
                }
            });

            return episodeResult;
        }

        public void ClearEpisodes()
        {
            if (core == null) return;

            episodeView.ItemsSource = null;
            epView.MyEpisodeResultCollection.Clear();
            RecomendationLoaded.IsVisible = true;
            episodeView.ItemsSource = epView.MyEpisodeResultCollection;
            SetHeight();
        }


        void SetHeight(bool? setNull = null, int? overrideCount = null)
        {
            if (core == null) return;

            episodeView.RowHeight = Settings.EpDecEnabled ? 170 : 100;
            Device.BeginInvokeOnMainThread(() => episodeView.HeightRequest = ((setNull ?? showState != 0) ? 0 : ((overrideCount ?? epView.MyEpisodeResultCollection.Count) * (episodeView.RowHeight) + 40)));
        }

        private async void TrailerBtt_Clicked(object sender, EventArgs e)
        {
            if (core == null) return;

            if (trailerUrl != null) {
                if (trailerUrl != "") {
                    await App.RequestVlc(trailerUrl, currentMovie.title.name + " - Trailer");
                    //  App.PlayVLCWithSingleUrl(trailerUrl, currentMovie.title.name + " - Trailer");
                }
            }
        }

        async void FadeTitles(bool fadeSeason)
        {
            if (core == null) return;

            print("FAFSAFAFAFA:::");
            DescriptionLabel.Opacity = 0;
            RatingLabel.Opacity = 0;
            RatingLabelRating.Opacity = 0;
            SeasonBtt.Opacity = 0;
            //Rectangle bounds = DescriptionLabel.Bounds;
            // DescriptionLabel.LayoutTo(new Rectangle(bounds.X, bounds.Y, bounds.Width, 0), FATE_TIME_MS);
            await RatingLabelRating.FadeTo(1, FATE_TIME_MS);
            ReviewLabel.FadeTo(1, FATE_TIME_MS);

            await RatingLabel.FadeTo(1, FATE_TIME_MS);
            await DescriptionLabel.FadeTo(1, FATE_TIME_MS);
            //    await DescriptionLabel.LayoutTo(bounds, FATE_TIME_MS);
            if (fadeSeason) {
                await SeasonBtt.FadeTo(1, FATE_TIME_MS);
            }
        }

        /*
        public async void AnimateInTrailer()
        {
            
            await Task.Delay(5000);
            TrailerBtt.HeightRequest = 200;
            Gradient.HeightRequest = 200;
            TrailerBtt.Opacity = 0;
            TrailerBtt.FadeTo(1);
            NormalStack.TranslateTo(0, 200,500);
        }*/

        bool setFirstEpAsFade = false;
        private void MovieResult_titleLoaded(object sender, Movie e)
        {
            if (core == null) return;

            if (loadedTitle) return;
            if (e.title.name != mainPoster.name) return;

            loadedTitle = true;
            isMovie = (e.title.movieType == MovieType.Movie || e.title.movieType == MovieType.AnimeMovie);

            if (setKey) {
                SetKey();
            }
            MainThread.BeginInvokeOnMainThread(() => {
                //App.ShowNotIntent("NEW EPISODE - " + currentMovie.title.name, currentMovie.title.name, 1337, currentMovie.title.id, currentMovie.title.name, bigIconUrl: currentMovie.title.hdPosterUrl, time: DateTime.UtcNow.AddSeconds(1));//ShowNotification("NEW EPISODE - " + currentMovie.title.name, setNotificationsTimes[i].episodeName, _id, i * 10);

                EPISODES.Text = isMovie ? "MOVIE" : "EPISODES";
                setFirstEpAsFade = true;

                try {
                    string souceUrl = e.title.trailers.First().PosterUrl;
                    if (CheckIfURLIsValid(souceUrl)) {
                        TrailerBtt.Opacity = 0;
                        TrailerBtt.FadeTo(1);
                        TrailerBtt.Source = souceUrl;
                        setFirstEpAsFade = false;
                    }
                    else {
                        TrailerBtt.Source = ImageSource.FromResource("CloudStreamForms.Resource.gradient.png", Assembly.GetExecutingAssembly());
                    }
                }
                catch (Exception) {
                    TrailerBtt.Source = ImageSource.FromResource("CloudStreamForms.Resource.gradient.png", Assembly.GetExecutingAssembly());
                }

                ChangeStar();

                string extra = "";
                bool haveSeasons = e.title.seasons != 0;

                if (haveSeasons) {
                    extra = e.title.seasons + " Season" + (e.title.seasons == 1 ? "" : "s") + " | ";
                }

                string rYear = mainPoster.year;
                if (rYear == null || rYear == "") {
                    rYear = e.title.year;
                }
                RatingLabel.Text = ((rYear + " | " + e.title.runtime).Replace("|  |", "|")).Replace("|", "  "); //+ " | " + extra + "★ " + e.title.rating).Replace("|  |", "|")).Replace("|", "  ");
                RatingLabelRating.Text = "Rated: " + e.title.rating;
                DescriptionLabel.Text = Settings.MovieDecEnabled ? CloudStreamCore.RemoveHtmlChars(e.title.description) : "";
                if (e.title.description == "") {
                    DescriptionLabel.HeightRequest = 0;
                }

                // ---------------------------- SEASONS ----------------------------


                SeasonPicker.IsVisible = haveSeasons;
                FadeTitles(haveSeasons);

                DubPicker.SelectedIndexChanged += DubPicker_SelectedIndexChanged;
                if (haveSeasons) {
                    List<string> season = new List<string>();
                    for (int i = 1; i <= e.title.seasons; i++) {
                        season.Add("Season " + i);
                    }
                    SeasonPicker.ItemsSource = season;

                    int selIndex = App.GetKey<int>("SeasonIndex", core.activeMovie.title.id, 0);
                    try {
                        SeasonPicker.SelectedIndex = Math.Min(selIndex, SeasonPicker.ItemsSource.Count - 1);
                    }
                    catch (Exception) {
                        SeasonPicker.SelectedIndex = 0; // JUST IN CASE
                    }

                    currentSeason = SeasonPicker.SelectedIndex + 1;


                    print("GetImdbEpisodes>>>>>>>>>>>>>>><<");
                    core.GetImdbEpisodes(currentSeason);
                }
                else {
                    currentSeason = 0; // MOVIES
                    core.GetImdbEpisodes();
                }
                SeasonPicker.SelectedIndexChanged += SeasonPicker_SelectedIndexChanged;

                // ---------------------------- RECOMMENDATIONS ----------------------------

                /*
                foreach (var item in Recommendations.Children) { // SETUP
                    Grid.SetColumn(item, 0);
                    Grid.SetRow(item, 0);
                }*/
                Recommendations.Children.Clear();
                for (int i = 0; i < RecomendedPosters.Count; i++) {
                    Poster p = e.title.recomended[i];
                    string posterURL = ConvertIMDbImagesToHD(p.posterUrl, 76, 113, 1.75); //.Replace(",76,113_AL", "," + pwidth + "," + pheight + "_AL").Replace("UY113", "UY" + pheight).Replace("UX76", "UX" + pwidth);
                    if (CheckIfURLIsValid(posterURL)) {
                        Grid stackLayout = new Grid() { VerticalOptions = LayoutOptions.Start };
                        Button imageButton = new Button() { HeightRequest = RecPosterHeight, WidthRequest = RecPosterWith, BackgroundColor = Color.Transparent, VerticalOptions = LayoutOptions.Start };
                        var ff = new FFImageLoading.Forms.CachedImage {
                            Source = posterURL,
                            HeightRequest = RecPosterHeight,
                            WidthRequest = RecPosterWith,
                            BackgroundColor = Color.Transparent,
                            VerticalOptions = LayoutOptions.Start,
                            Transformations = {
                            //  new FFImageLoading.Transformations.RoundedTransformation(10,1,1.5,10,"#303F9F")
                            new FFImageLoading.Transformations.RoundedTransformation(1, 1, 1.5, 0, "#303F9F")
                        },
                            InputTransparent = true,
                        };

                        // ================================================================ RECOMMENDATIONS CLICKED ================================================================
                        stackLayout.SetValue(XamEffects.TouchEffect.ColorProperty, Color.White);
                        Commands.SetTap(stackLayout, new Command((o) => {
                            int z = (int)o;
                            if (Search.mainPoster.url != RecomendedPosters[z].url) {
                                /*if (lastMovie == null) {
                                    lastMovie = new List<Movie>();
                                }
                                lastMovie.Add(core.activeMovie);*/
                                Search.mainPoster = RecomendedPosters[z];
                                Page _p = new MovieResult();// { mainPoster = mainPoster };
                                Navigation.PushModalAsync(_p);
                            }
                            //do something
                        }));
                        Commands.SetTapParameter(stackLayout, i);
 
                        stackLayout.Children.Add(ff);
                        stackLayout.Children.Add(imageButton);

                        Recommendations.Children.Add(stackLayout);
                    }
                }
                SetRecs();
            });
        }

        const double _RecPosterMulit = 1.75;
        const int _RecPosterHeight = 100;
        const int _RecPosterWith = 65;
        int RecPosterHeight { get { return (int)Math.Round(_RecPosterHeight * _RecPosterMulit); } }
        int RecPosterWith { get { return (int)Math.Round(_RecPosterWith * _RecPosterMulit); } }

        void SetRecs()
        {
            if (core == null) return;

            Device.BeginInvokeOnMainThread(() => {
                const int total = 12;
                int perCol = (Application.Current.MainPage.Width < Application.Current.MainPage.Height) ? 3 : 6;

                for (int i = 0; i < Recommendations.Children.Count; i++) { // GRID
                    Grid.SetColumn(Recommendations.Children[i], i % perCol);
                    Grid.SetRow(Recommendations.Children[i], (int)Math.Floor(i / (double)perCol));
                }
                // Recommendations.HeightRequest = (RecPosterHeight + Recommendations.RowSpacing) * (total / perCol);
                Recommendations.HeightRequest = (RecPosterHeight + Recommendations.RowSpacing) * (total / perCol) - 7 + Recommendations.RowSpacing;
            });
        }

        private void DubPicker_SelectedIndexChanged(object sender, int e)
        {
            if (core == null) return;

            print("DUBCHANGED::");
            try {
                isDub = "Dub" == DubPicker.ItemsSource[DubPicker.SelectedIndex];
                SetDubExist();
            }
            catch (Exception _ex) {
                print("EXPECTION:" + _ex);
            }
        }

        private void SeasonPicker_SelectedIndexChanged(object sender, int e)
        {
            ClearEpisodes();
            //  epView.MyEpisodeResultCollection.Clear();
            ChangeBatchDownload();

            DubPicker.button.FadeTo(0, FATE_TIME_MS);
            currentSeason = SeasonPicker.SelectedIndex + 1;
            App.SetKey("SeasonIndex", core.activeMovie.title.id, SeasonPicker.SelectedIndex);

            core.GetImdbEpisodes(currentSeason);
        }

        void SetChangeTo(int maxEp = -1)
        {
            Device.BeginInvokeOnMainThread(() => {
                if (maxEp == -1) {
                    maxEp = maxEpisodes;
                }
                var source = new List<string>();

                int times = (int)Math.Ceiling((decimal)epView.AllEpisodes.Length / (decimal)MovieResultMainEpisodeView.MAX_EPS_PER);

                for (int i = 0; i < times; i++) {
                    int fromTo = maxEp - i * MovieResultMainEpisodeView.MAX_EPS_PER;
                    string f = (i * MovieResultMainEpisodeView.MAX_EPS_PER + 1) + "-" + ((i) * MovieResultMainEpisodeView.MAX_EPS_PER + Math.Min(fromTo, MovieResultMainEpisodeView.MAX_EPS_PER));
                    source.Add(f);
                }

                FromToPicker = new LabelList(FromToBtt, source, "Select Episode") {
                    SelectedIndex = 0//.IsVisible = FromToPicker.ItemsSource.Count > 1;           
                };//.IsVisible = FromToPicker.ItemsSource.Count > 1;                
                FromToPicker.IsVisible = FromToPicker.ItemsSource.Count > 1;
                FromToPicker.button.IsEnabled = FromToPicker.ItemsSource.Count > 1;
                FromToPicker.SelectedIndexChanged += (o, e) => {
                    SetEpisodeFromTo(e, maxEpisodes);
                };
            });
        }

        private void EpisodesHalfLoaded(object sender, List<Episode> e)
        {
            if (e.Count > 0) {
                if (setFirstEpAsFade) {
                    Device.BeginInvokeOnMainThread(() => {
                        TrailerBtt.Source = CloudStreamCore.ConvertIMDbImagesToHD(e[0].posterUrl, 356, 200);
                        TrailerBtt.Opacity = 0;
                        TrailerBtt.FadeTo(1);
                    });
                }
            }
        }

        void ChangeBatchDownload()
        {
            bool canBatchDownload = epView.MyEpisodeResultCollection.Count > 1;
            BatchDownloadBtt.IsEnabled = canBatchDownload;
            BatchDownloadBtt.FadeTo(canBatchDownload ? 1 : 0);
        }

        private void EpisodesLoaded(object sender, List<Episode> e)
        {
            if (core == null) return;

            Device.BeginInvokeOnMainThread(() => {
                print("GOT RESULTS; LETS GO");

                if (e == null || e.Count == 0) {
                    RecomendationLoaded.IsVisible = false;
                    return;
                };
                print("episodes loaded");

                ClearEpisodes();

                //bool isLocalMovie = false;
                bool isAnime = currentMovie.title.movieType == MovieType.Anime;

                if (currentMovie.title.movieType != MovieType.Movie && currentMovie.title.movieType != MovieType.AnimeMovie) { // SEASON ECT
                    print("MAXEPS:::" + CurrentEpisodes.Count);
                    epView.AllEpisodes = new EpisodeResult[CurrentEpisodes.Count];
                    maxEpisodes = epView.AllEpisodes.Length;
                    for (int i = 0; i < CurrentEpisodes.Count; i++) {
                        AddEpisode(new EpisodeResult() { Episode = i + 1, IMDBEpisodeId = CurrentEpisodes[i].id, Title = CurrentEpisodes[i].name, Id = i, Description = CurrentEpisodes[i].description.Replace("\n", "").Replace("  ", ""), PosterUrl = CurrentEpisodes[i].posterUrl, Rating = CurrentEpisodes[i].rating, Progress = 0, epVis = false, }, i);
                    }
                    if (!isAnime) {
                        SetEpisodeFromTo(0);
                        SetChangeTo();
                    }
                }
                else { // MOVE
                    maxEpisodes = 1;
                    epView.AllEpisodes = new EpisodeResult[1];
                    AddEpisode(new EpisodeResult() { Title = currentMovie.title.name, IMDBEpisodeId = currentMovie.title.id, Description = currentMovie.title.description, Id = 0, PosterUrl = "", Progress = 0, Rating = "", epVis = false }, 0);
                    SetEpisodeFromTo(0);
                }
                ChangeBatchDownload();

                DubPicker.ItemsSource.Clear();

                // SET DUB SUB
                if (isAnime) {
                    core.GetSubDub(currentSeason, out bool subExists, out bool dubExists);

                    isDub = dubExists;

                    if (Settings.DefaultDub) {
                        if (dubExists) {
                            DubPicker.ItemsSource.Add("Dub");
                        }
                    }
                    if (subExists) {
                        DubPicker.ItemsSource.Add("Sub");
                    }
                    if (!Settings.DefaultDub) {
                        if (dubExists) {
                            DubPicker.ItemsSource.Add("Dub");
                        }
                    }

                    if (DubPicker.ItemsSource.Count > 0) {
                        DubPicker.SelectedIndex = 0;
                    }
                    DubPicker.OnUpdateList();
                    SetDubExist();
                }

                bool enabled = currentMovie.title.movieType == MovieType.Anime; //CurrentMalLink != "";
                print("SETACTIVE::: " + enabled);

                /*
                MALB.IsVisible = enabled;
                MALB.IsEnabled = enabled;
                MALA.IsVisible = enabled;
                MALA.IsEnabled = enabled;

                Grid.SetColumn(MALB, enabled ? 5 : 0);
                Grid.SetColumn(MALA, enabled ? 5 : 0);*/

                DubPicker.button.Opacity = 0;
                DubPicker.IsVisible = DubPicker.ItemsSource.Count > 0;
                DubPicker.button.FadeTo(DubPicker.IsVisible ? 1 : 0, FATE_TIME_MS);
                // ForceUpdate();
            });
        }

        readonly static Dictionary<string, bool> GetLatestDub = new Dictionary<string, bool>();

        void SetDubExist()
        {
            print("SETDUB:::");
            print("SETDUB:::SET");

            TempThread tempThred = core.CreateThread(6);
            core.StartThread("Set SUB/DUB", () => {
                try {
                    if (core == null) return;
                    int max = core.GetMaxEpisodesInAnimeSeason(currentSeason, isDub, tempThred);
                    if (max > 0) {
                        print("CLEAR AND ADD");
                        MainThread.BeginInvokeOnMainThread(() => {
                            if (core == null) return;

                            maxEpisodes = max;
                            print("MAXUSsssss" + maxEpisodes + "|" + max + "|" + (int)Math.Ceiling((double)max / (double)MovieResultMainEpisodeView.MAX_EPS_PER));

                            SetEpisodeFromTo(0, max);
                            SetChangeTo(max);

                            // CLEAR EPISODES SO SWITCHING SUB DUB 
                            if (GetLatestDub.ContainsKey(currentMovie.title.id)) {
                                if (GetLatestDub[currentMovie.title.id] != isDub) {
                                    try {
                                        for (int i = 0; i < epView.MyEpisodeResultCollection.Count; i++) {
                                            if (epView.MyEpisodeResultCollection[i].GetHasLoadedLinks()) {
                                                print("CLEAR OS : " + i);
                                                epView.MyEpisodeResultCollection[i].ClearMirror();
                                            }
                                        }
                                    }
                                    catch (Exception _ex) {
                                        print("MAIN ERROR IN CLEAR: " + _ex);
                                    }
                                }
                            }
                            GetLatestDub[currentMovie.title.id] = isDub;
                            ChangeBatchDownload();
                        });
                    }
                    else {
                        Device.BeginInvokeOnMainThread(() => {
                            RecomendationLoaded.IsVisible = false;
                        });
                    }
                }
                finally {
                    if (core != null) {
                        core.JoinThred(tempThred);
                    }
                }
            });
        }


        private void MovieResult_trailerLoaded(object sender, List<Trailer> e)
        {
            if (core == null) return;

            if (e == null) return;
            epView.CurrentTrailers.Clear();
            for (int i = 0; i < e.Count; i++) {
                epView.CurrentTrailers.Add(e[i]);
            }

            if (e.Count > 4) return; // MAX 4 TRAILERS

            if (trailerUrl == "") {
                trailerUrl = e[0].Url;
            }

            Device.BeginInvokeOnMainThread(() => {
                TRAILERSTAB.IsVisible = true;
                TRAILERSTAB.IsEnabled = true;
                trailerView.Children.Clear();
                trailerView.HeightRequest = e.Count * 240 + 200;
                if (PlayBttGradient.Source == null) {
                    PlayBttGradient.Source = GetImageSource("nexflixPlayBtt.png");
                    PlayBttGradient.Opacity = 0;
                    PlayBttGradient.FadeTo(1, FATE_TIME_MS);
                }

                for (int i = 0; i < e.Count; i++) {
                    string p = e[i].PosterUrl;
                    if (CheckIfURLIsValid(p)) {
                        Grid stackLayout = new Grid();
                        Label textLb = new Label() { Text = e[i].Name, TextColor = Color.FromHex("#e70000"), FontAttributes = FontAttributes.Bold, FontSize = 15, TranslationX = 10 };
                        Image playBtt = new Image() { Source = GetImageSource("nexflixPlayBtt.png"), VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Scale = 0.5, InputTransparent = true };
                        var ff = new FFImageLoading.Forms.CachedImage {
                            Source = p,
                            BackgroundColor = Color.Transparent,
                            VerticalOptions = LayoutOptions.Fill,
                            Aspect = Aspect.AspectFill,
                            HorizontalOptions = LayoutOptions.Fill,
                            Transformations = {
                            new FFImageLoading.Transformations.RoundedTransformation(1, 1.7, 1, 0, "#303F9F")

                        },
                            InputTransparent = true,
                        };

                        int _sel = int.Parse(i.ToString());
                        stackLayout.Children.Add(ff);
                        stackLayout.Children.Add(playBtt);
                        trailerView.Children.Add(stackLayout);
                        trailerView.Children.Add(textLb);

                        stackLayout.SetValue(XamEffects.TouchEffect.ColorProperty, new Color(1, 1, 1, 0.3));
                        Commands.SetTap(stackLayout, new Command(async (o) => {
                            int z = (int)o;
                            var _t = epView.CurrentTrailers[z];
                            await RequestVlc(_t.Url, _t.Name);
                            //PlayVLCWithSingleUrl(_t.Url, _t.Name);
                        }));
                        Commands.SetTapParameter(stackLayout, _sel);
                        Grid.SetRow(stackLayout, (i + 1) * 2 - 2);
                        Grid.SetRow(textLb, (i + 1) * 2 - 1);

                    }
                }
            });

            //  trailerView.Children.Add(new )
            /*
            MainThread.BeginInvokeOnMainThread(() => {
                Trailer trailer = activeMovie.title.trailers.First();
                trailerUrl = trailer.url;
                print(trailer.posterUrl);
                TrailerBtt.Source = trailer.posterUrl;//ImageSource.FromUri(new System.Uri(trailer.posterUrl));

            });*/

        }
        private async void IMDb_Clicked(object sender, EventArgs e)
        {
            // if (!SameAsActiveMovie()) return;
            List<string> options = new List<string>() {
                "IMDb",
            };
            if (CurrentMalLink.IsClean()) {
                options.Add("MAL");
            }
            if (CurrentAniListLink.IsClean()) {
                options.Add("AniList");
            }
            string option = await ActionPopup.DisplayActionSheet("Open", options.ToArray());
            if (option == "IMDb") {
                await App.OpenBrowser("https://www.imdb.com/title/" + mainPoster.url);
            }
            else if (option == "MAL") {
                await App.OpenBrowser(CurrentMalLink);
            }
            else if (option == "AniList") {
                await App.OpenBrowser(CurrentAniListLink);
            }
        }
        private void MAL_Clicked(object sender, EventArgs e)
        {
            //   if (!SameAsActiveMovie()) return;
        }

        void PlayDownloadedEp(EpisodeResult episodeResult, string data = null)
        {
            var _info = App.GetDownloadInfo(GetCorrectId(episodeResult), false);
            //   var downloadKeyData = data ?? _info.info.fileUrl;
            SetEpisode(episodeResult);
            Download.PlayDownloadedFile(_info);
            //  Download.PlayVLCFile(downloadKeyData, episodeResult.Title, GetCorrectId(episodeResult).ToString());
        }

        /*
        void PlayEpisodeRes(EpisodeResult episodeResult)
        {
            string hasDownloadedFile = App.GetKey("Download", GetId(episodeResult), "");
            if (hasDownloadedFile != "") {
                Download.PlayFile(hasDownloadedFile, episodeResult.Title);
            }
            else {
                LoadLinksForEpisode(episodeResult);
            }
        }*/

        /*
    private void ImageButton_Clicked(object sender, EventArgs e) // LOAD
    {
        if (!SameAsActiveMovie()) return;
        EpisodeResult episodeResult = ((EpisodeResult)((ImageButton)sender).BindingContext);
        PlayEpisodeRes(episodeResult);

        episodeView.SelectedItem = null;
    }*/

        bool loadingLinks = false;

        static int currentDownloadSearchesHappening = 0;

        async Task<EpisodeResult> LoadLinksForEpisode(EpisodeResult episodeResult, bool autoPlay = true, bool overrideLoaded = false, bool showloading = true)
        {
            if (loadingLinks) return episodeResult;

            if (episodeResult.GetHasLoadedLinks()) {
                if (autoPlay) { PlayEpisode(episodeResult); }
            }
            else {
                core.GetEpisodeLink(isMovie ? -1 : (episodeResult.Id + 1), currentSeason, isDub: isDub, purgeCurrentLinkThread: currentDownloadSearchesHappening > 0);

                await Device.InvokeOnMainThreadAsync(async () => {
                    // NormalStack.IsEnabled = false;
                    loadingLinks = true;
                    if (showloading) {
                        await ActionPopup.DisplayLoadingBar(LoadingMiliSec, "Loading Links...");
                    }
                    else {
                        await Task.Delay(LoadingMiliSec);
                    }

                    /*
                    UserDialogs.Instance.ShowLoading("Loading links...", MaskType.Gradient);
                    await Task.Delay(LoadingMiliSec);
                    UserDialogs.Instance.HideLoading();*/
                    int errorCount = 0;
                    const int maxErrorcount = 1;
                    bool gotError = false;
                    loadingLinks = false;


                //NormalStack.IsEnabled = true;
                // NormalStack.Opacity = 1f;
                checkerror:;
                    if (episodeResult == null) {
                        gotError = true;
                    }
                    else {
                        if (episodeResult.GetMirrosUrls() == null) {
                            gotError = true;
                        }
                        else {
                            if (episodeResult.GetMirrosUrls().Count > 0) {
                                if (autoPlay) { PlayEpisode(episodeResult); }
                                //episodeResult.GetHasLoadedLinks() = true;
                            }
                            else {
                                gotError = true;
                            }
                        }
                    }
                    if (gotError) {
                        if (errorCount < maxErrorcount) {
                            errorCount++;
                            if (showloading) {
                                await ActionPopup.DisplayLoadingBar(2000, "Loading More Links...");
                            }
                            else {
                                await Task.Delay(2000);
                            }
                            goto checkerror;
                        }
                        else {
                            episodeView.SelectedItem = null;
                            if (showloading) {
                                App.ShowToast(errorEpisodeToast);
                            }
                        }
                    }
                });
            }

            return episodeResult;
        }

        void Dispose()
        {
            core = null;
        }

        static bool isRequestingPlayEpisode = false;

        // ============================== PLAY VIDEO ==============================
        async void PlayEpisode(EpisodeResult episodeResult, bool? overrideSelectVideo = null)
        {
            if (isRequestingPlayEpisode) return;
            isRequestingPlayEpisode = true;

            string id = episodeResult.IMDBEpisodeId;
            if (id != "") {
                if (ViewHistory) {
                    App.SetKey(App.VIEW_HISTORY, id, true);
                    SetColor(episodeResult);
                    // FORCE UPDATE
                    ForceUpdate(episodeResult.Id);
                }
            }

            /*
            string _sub = "";
            if (currentMovie.subtitles != null) {
                if (currentMovie.subtitles.Count > 0) {
                    _sub = currentMovie.subtitles[0].data;
                }
            }*/
            if (currentMovie.subtitles == null) {
                core.activeMovie.subtitles = new List<Subtitle>();
            }

            bool useVideo = overrideSelectVideo ?? Settings.UseVideoPlayer;
            if (useVideo) {
                if (VideoPage.isShown) {
                    VideoPage.isShown = false;
                    VideoPage.changeFullscreenWhenPop = false;
                    await Navigation.PopModalAsync(true);
                    await Task.Delay(30);
                }
                VideoPage.loadLinkValidForHeader = currentMovie.title.id;

                VideoPage.loadLinkForNext = async (t, load) => {
                    return await LoadLinkFrom(t, load);
                };

                VideoPage.canLoad = (t) => {
                    return CanLoadLinkFrom(t, out int index);
                };

                VideoPage.maxEpisodeForLoading = maxEpisodes;
            }
            await App.RequestVlc(episodeResult.GetMirrosUrls(), episodeResult.GetMirros(), episodeResult.OgTitle, episodeResult.IMDBEpisodeId, episode: episodeResult.Episode, season: currentSeason, subtitleFull: currentMovie.subtitles.Select(t => t.data).FirstOrDefault(), descript: episodeResult.Description, overrideSelectVideo: overrideSelectVideo, startId: (int)episodeResult.ProgressState, headerId: currentMovie.title.id, isFromIMDB: true);// startId: FROM_PROGRESS); //  (int)episodeResult.ProgressState																																																																													  //App.PlayVLCWithSingleUrl(episodeResult.mirrosUrls, episodeResult.Mirros, currentMovie.subtitles.Select(t => t.data).ToList(), currentMovie.subtitles.Select(t => t.name).ToList(), currentMovie.title.name, episodeResult.Episode, currentSeason, overrideSelectVideo);
            isRequestingPlayEpisode = false;
        }

        public bool CanLoadLinkFrom(string id, out int index)
        {
            index = 0;
            if (core == null) return false;
            index = (epView.MyEpisodeResultCollection.Select(t => t.IMDBEpisodeId).IndexOf(id));
            if (index == -1) return false;
            if (epView.MyEpisodeResultCollection.Count - 1 <= index) { // NEXT EPISODE DOSENT EXIST
                return false;
            }
            return true;
        }

        public async Task<string> LoadLinkFrom(string id, bool load)
        {
            if (!CanLoadLinkFrom(id, out int index)) return "";
            var _ep = epView.MyEpisodeResultCollection[index + 1];
            await LoadLinksForEpisode(_ep, load, false, load);
            return _ep.IMDBEpisodeId;
        }

        // ============================== FORCE UPDATE ==============================
        void ForceUpdate(int? item = null, bool checkColor = false)
        {
            if (core == null) return;
            if (epView == null) return;
            if (epView.MyEpisodeResultCollection.Count == 0) return;
            //return;
            print("FORCE UPDATING");
            var _e = epView.MyEpisodeResultCollection.ToList();
            Device.BeginInvokeOnMainThread(() => {
                if (core == null) return;
                epView.MyEpisodeResultCollection.Clear();
                for (int i = 0; i < _e.Count; i++) { 
                    epView.MyEpisodeResultCollection.Add(UpdateLoad((EpisodeResult)_e[i].Clone(), checkColor));
                }
            });
        }

        async Task EpisodeSettings(EpisodeResult episodeResult)
        {
            if (loadingLinks) return;

            print("EPDATA:::" + episodeResult.OgTitle + "|" + episodeResult.Episode);
            if (!episodeResult.GetHasLoadedLinks()) {
                try {
                    await LoadLinksForEpisode(episodeResult, false);
                }
                catch (Exception) { }
            }
            /*
            if (loadingLinks) {
                await Task.Delay(LoadingMiliSec + 40);
            }*/

            if (!episodeResult.GetHasLoadedLinks()) {
                //   App.ShowToast(errorEpisodeToast); episodeView.SelectedItem = null;
                return;
            }

            // ============================== GET ACTION ==============================
            string action = "";

            bool hasDownloadedFile = App.KeyExists("dlength", "id" + GetCorrectId(episodeResult));
            string downloadKeyData = "";

            List<string> actions = new List<string>() { "Play in App", "Play in Browser", "Auto Download", "Download", "Download Subtitles", "Copy Link", "Reload" }; // "Remove Link",

            if (App.CanPlayExternalPlayer()) {
                actions.Insert(1, "Play External App");
            }

            if (hasDownloadedFile) {
                downloadKeyData = App.GetDownloadInfo(GetCorrectId(episodeResult), false).info.fileUrl;//.GetKey("Download", GetId(episodeResult), "");
                print("INFOOOOOOOOO:::" + downloadKeyData);
                actions.Add("Play Downloaded File"); actions.Add("Delete Downloaded File");
            }
            if (MainChrome.IsConnectedToChromeDevice) {
                actions.Insert(0, "Chromecast");
                actions.Insert(1, "Chromecast mirror");
            }

            action = await ActionPopup.DisplayActionSheet(episodeResult.Title, actions.ToArray());//await DisplayActionSheet(episodeResult.Title, "Cancel", null, actions.ToArray());

            async void ChromecastAt(int count)
            {
                chromeResult = episodeResult;
                chromeMovieResult = currentMovie;
                bool succ = false;
                count--;
                episodeView.SelectedItem = null;

                while (!succ) {
                    count++;

                    if (count >= episodeResult.GetMirros().Count) {
                        succ = true;
                    }
                    else {
                        succ = await MainChrome.CastVideo(episodeResult.GetMirrosUrls()[count], episodeResult.GetMirros()[count], subtitleUrl: "", posterUrl: currentMovie.title.hdPosterUrl, movieTitle: currentMovie.title.name, subtitleDelay: 0);
                    }
                }
                ChromeCastPage.currentSelected = count;

            }

            if (action == "Play in Browser") {
                string copy = await ActionPopup.DisplayActionSheet("Open Link", episodeResult.GetMirros().ToArray()); // await DisplayActionSheet("Open Link", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < episodeResult.GetMirros().Count; i++) {
                    if (episodeResult.GetMirros()[i] == copy) {
                        App.OpenSpecifiedBrowser(episodeResult.GetMirrosUrls()[i]);
                    }
                }
            }
            else if (action == "Remove Link") {
                string rLink = await ActionPopup.DisplayActionSheet("Remove Link", episodeResult.GetMirros().ToArray()); //await DisplayActionSheet("Download", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < episodeResult.GetMirros().Count; i++) {
                    if (episodeResult.GetMirros()[i] == rLink) {
                        App.ShowToast("Removed " + episodeResult.GetMirros()[i]);
                        episodeResult.GetMirrosUrls().RemoveAt(i);
                        episodeResult.GetMirros().RemoveAt(i);
                        await EpisodeSettings(episodeResult);
                        break;
                    }
                }
            }
            else if (action == "Chromecast") { // ============================== CHROMECAST ==============================
                ChromecastAt(0);
                print("CASTOS");
            }
            else if (action == "Chromecast mirror") {
                string subMirror = await ActionPopup.DisplayActionSheet("Cast Mirror", episodeResult.GetMirros().ToArray());//await DisplayActionSheet("Copy Link", "Cancel", null, episodeResult.Mirros.ToArray());
                ChromecastAt(episodeResult.GetMirros().IndexOf(subMirror));
            }
            else if (action == "Play") { // ============================== PLAY ==============================
                PlayEpisode(episodeResult);
            }
            else if (action == "Play External App") {
                PlayEpisode(episodeResult, false);
            }
            else if (action == "Play in App") {
                PlayEpisode(episodeResult, true);
            }
            else if (action == "Copy Link") { // ============================== COPY LINK ==============================
                string copy = await ActionPopup.DisplayActionSheet("Copy Link", episodeResult.GetMirros().ToArray());//await DisplayActionSheet("Copy Link", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < episodeResult.GetMirros().Count; i++) {
                    if (episodeResult.GetMirros()[i] == copy) {
                        await Clipboard.SetTextAsync(episodeResult.GetMirrosUrls()[i]);
                        App.ShowToast("Copied Link to Clipboard");
                        break;
                    }
                }
            }
            else if (action == "Auto Download") {
                int epId = GetCorrectId(episodeResult);
                BasicLink[] info = null;
                bool hasMirrors = false;
                var baseLinks = CloudStreamCore.GetCachedLink(episodeResult.IMDBEpisodeId);
                if (baseLinks.HasValue) {
                    info = baseLinks.Value.links.Where(t => t.CanBeDownloaded).ToList().OrderHDLinks().ToArray();
                    hasMirrors = info.Length > 0;
                }
                if (hasMirrors && info != null) {
                    App.ShowToast("Download Started");
                    App.UpdateDownload(epId, -1);
                    string dpath = App.RequestDownload(epId, episodeResult.OgTitle, episodeResult.Description, episodeResult.Episode, currentSeason, info.Select(t => { return new BasicMirrorInfo() { mirror = t.baseUrl, name = t.PublicName, referer = t.referer }; }).ToList(), episodeResult.GetDownloadTitle(currentSeason, episodeResult.Episode) + ".mp4", episodeResult.PosterUrl, currentMovie.title, episodeResult.IMDBEpisodeId);
                    episodeResult.downloadState = 2; // SET IS DOWNLOADING
                    ForceUpdate();
                }
                else {
                    App.ShowToast("Download Failed, No Mirrors Found");
                    episodeResult.downloadState = 0;
                    ForceUpdate();
                }
            }
            else if (action == "Download") {  // ============================== DOWNLOAD FILE ==============================
                List<BasicLink> links = episodeResult.GetBasicLinks().Where(t => t.CanBeDownloaded).ToList();

                string download = await ActionPopup.DisplayActionSheet("Download", links.Select(t => t.PublicName).ToArray()); //await DisplayActionSheet("Download", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < links.Count; i++) {
                    if (links[i].PublicName == download) {
                        var link = links[i];
                        DownloadSubtitlesToFileLocation(episodeResult, currentMovie, currentSeason, showToast: false);
                        TempThread tempThred = core.CreateThread(4);
                        core.StartThread("DownloadThread", async () => {
                            try {
                                //UserDialogs.Instance.ShowLoading("Checking link...", MaskType.Gradient);
                                await ActionPopup.StartIndeterminateLoadinbar("Checking link...");
                                double fileSize = CloudStreamCore.GetFileSize(link.baseUrl, link.referer ?? "");
                                //    UserDialogs.Instance.HideLoading();
                                await ActionPopup.StopIndeterminateLoadinbar();
                                if (fileSize > 1) {
                                    print("DSUZE:::::" + episodeResult.Episode);

                                    // ImageService.Instance.LoadUrl(episodeResult.PosterUrl, TimeSpan.FromDays(30)); // CASHE IMAGE
                                    App.UpdateDownload(GetCorrectId(episodeResult), -1);
                                    print("CURRENTSESON: " + currentSeason);

                                    string dpath = App.RequestDownload(GetCorrectId(episodeResult), episodeResult.OgTitle, episodeResult.Description, episodeResult.Episode, currentSeason, new List<BasicMirrorInfo>() { new BasicMirrorInfo() { mirror = link.baseUrl, name = link.PublicName, referer = link.referer } }, episodeResult.GetDownloadTitle(currentSeason, episodeResult.Episode) + ".mp4", episodeResult.PosterUrl, currentMovie.title, episodeResult.IMDBEpisodeId);

                                    App.ShowToast("Download Started - " + fileSize + "MB");
                                    episodeResult.downloadState = 2;
                                    ForceUpdate(episodeResult.Id);
                                }
                                else {
                                    await EpisodeSettings(episodeResult);
                                    App.ShowToast("Download Failed");
                                    ForceUpdate(episodeResult.Id);
                                }
                            }
                            finally {
                                //UserDialogs.Instance.HideLoading();
                                core.JoinThred(tempThred);
                            }
                        });
                        break;
                    }
                }
            }
            else if (action == "Reload") { // ============================== RELOAD ==============================
                try {
                    episodeResult.ClearMirror();
                    await LoadLinksForEpisode(episodeResult, false, true);
                }
                catch (Exception) { }

                //await Task.Delay(LoadingMiliSec + 40);

                if (!episodeResult.GetHasLoadedLinks()) {
                    return;
                }
                await EpisodeSettings(episodeResult);
            }
            else if (action == "Play Downloaded File") { // ============================== PLAY FILE ==============================
                                                         //  bool succ = App.DeleteFile(info.info.fileUrl); 
                                                         //  Download.PlayVLCFile(downloadKeyData, episodeResult.Title);
                PlayDownloadedEp(episodeResult, downloadKeyData);
            }
            else if (action == "Delete Downloaded File") {  // ============================== DELETE FILE ==============================
                DeleteFile(downloadKeyData, episodeResult);
            }
            else if (action == "Download Subtitles") {  // ============================== DOWNLOAD SUBTITLE ==============================
                DownloadSubtitlesToFileLocation(episodeResult, currentMovie, currentSeason, true);
            }
            episodeView.SelectedItem = null;
        }

        readonly static Dictionary<string, bool> hasSubtitles = new Dictionary<string, bool>();

        static void DownloadSubtitlesToFileLocation(EpisodeResult episodeResult, Movie currentMovie, int currentSeason, bool renew = false, bool showToast = true)
        {
            string id = episodeResult.IMDBEpisodeId;
            if (!renew && hasSubtitles.ContainsKey(id)) {
                if (showToast) {
                    App.ShowToast("Subtitles Already Downloaded");
                }
                return;
            }
            TempThread tempThred = mainCore.CreateThread(4);
            mainCore.StartThread("Subtitle Download", () => {
                try {
                    if (id.Replace(" ", "") == "") {
                        if (showToast) {
                            App.ShowToast("Id not found");
                        }
                        return;
                    }

                    string s = mainCore.DownloadSubtitle(id, Settings.NativeSubShortName, false);
                    if (s == "") {
                        if (showToast) {
                            App.ShowToast("No Subtitles Found");
                        }
                        return;
                    }
                    else {
                        string extraPath = "/" + GetPathFromType(currentMovie.title.movieType);
                        if (!currentMovie.title.IsMovie) {
                            extraPath += "/" + CensorFilename(currentMovie.title.name);
                        }
                        App.DownloadFile(s, episodeResult.GetDownloadTitle(currentSeason, episodeResult.Episode) + ".srt", true, extraPath); // "/Subtitles" +
                        if (showToast) {
                            App.ShowToast("Subtitles Downloaded");
                        }
                        if (!hasSubtitles.ContainsKey(id)) {
                            hasSubtitles.Add(id, true);
                        }
                    }
                }
                finally {
                    mainCore.JoinThred(tempThred);
                }
            });
        }

        void DeleteFile(string downloadKeyData, EpisodeResult episodeResult)
        {
            App.DeleteFile(downloadKeyData);
            App.DeleteFile(downloadKeyData.Replace(".mp4", ".srt"));
            App.UpdateDownload(GetCorrectId(episodeResult), 2);
            Download.RemoveDownloadCookie(GetCorrectId(episodeResult));//.DeleteFileFromFolder(downloadKeyData, "Download", GetId(episodeResult));
            SetColor(episodeResult);
            ForceUpdate(episodeResult.Id);
        }

        public int GetCorrectId(EpisodeResult episodeResult, Movie? m = null)
        {
            m ??= currentMovie;
            return int.Parse(((m?.title.movieType == MovieType.TVSeries || m?.title.movieType == MovieType.Anime) ? m?.episodes[episodeResult.Id].id : m?.title.id).Replace("tt", ""));
        }


        // ============================== ID OF EPISODE ==============================
        public string GetId(EpisodeResult episodeResult)
        {
            return GetId(episodeResult, currentMovie);
        }

        public static string GetId(EpisodeResult episodeResult, Movie currentMovie)
        {
            try {
                return (currentMovie.title.movieType == MovieType.TVSeries || currentMovie.title.movieType == MovieType.Anime) ? currentMovie.episodes[episodeResult.Id].id : currentMovie.title.id;
            }
            catch (Exception _ex) {
                print("FATAL EX IN GETID: " + _ex);
                return episodeResult.Id + "Extra=" + ToDown(episodeResult.Title) + "=EndAll";
            }
        }

        // ============================== TOGGLE HAS SEEN EPISODE ==============================

        bool toggleViewState = false;
        private void ViewToggle_Clicked(object sender, EventArgs e)
        {
            toggleViewState = !toggleViewState;
            ChangeViewToggle();
        }

        void ChangeViewToggle()
        {
            ViewToggle.Source = toggleViewState ? "visibility.svg" : "visibility_off.svg";// GetImageSource((toggleViewState ? "viewOffIcon.png" : "viewOnIcon.png"));
            ViewToggle.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(toggleViewState ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
        }

        public void SetEpisode(EpisodeResult episodeResult)
        {
            string id = episodeResult.IMDBEpisodeId;
            SetEpisode(id);
            SetColor(episodeResult);
            ForceUpdate(episodeResult.Id);
        }

        public static void SetEpisode(string id)
        {
            App.SetKey(App.VIEW_HISTORY, id, true);
        }

        void ToggleEpisode(EpisodeResult episodeResult)
        {
            string id = episodeResult.IMDBEpisodeId;
            ToggleEpisode(id);
            SetColor(episodeResult);
            ForceUpdate(episodeResult.Id);
        }

        public static void ToggleEpisode(string id)
        {
            if (id != "") {
                if (App.KeyExists(App.VIEW_HISTORY, id)) {
                    App.RemoveKey(App.VIEW_HISTORY, id);
                }
                else {
                    SetEpisode(id);
                }
            }
        }



        // ============================== USED FOR SMALL VIDEO PLAY ==============================
        /*  private void Grid_LayoutChanged(object sender, EventArgs e)
          {
              var s = ((Grid)sender);
              Commands.SetTap(s, new Command((o) => {
                  var episodeResult = (EpisodeResult)s.BindingContext;
                  PlayEpisodeRes(episodeResult);
              }));
          }*/

        // ============================== SHOW SETTINGS OF VIDEO ==============================
        private async void ViewCell_Tapped(object sender, EventArgs e)
        {
            if (!canTapEpisode) return;
            canTapEpisode = false;
            try {
                EpisodeResult episodeResult = ((EpisodeResult)(((ViewCell)sender).BindingContext));

                if (toggleViewState) {
                    ToggleEpisode(episodeResult);
                    episodeView.SelectedItem = null;
                }
                else {
                    await EpisodeSettings(episodeResult);
                }
            }
            finally {
                canTapEpisode = true;
            }
        }



        #region ===================================================== MOVE RECOMENDATION ECT BAR  =====================================================
        /// <summary>
        /// 0 = episodes, 1 = recommendations, 2 = trailers
        /// </summary>
        int showState = 0;
        int prevState = 0;
        void ChangedRecState(int state, bool overrideCheck = false)
        {
            prevState = int.Parse(showState.ToString());
            if (state == showState && !overrideCheck) return;
            showState = state;
            Device.BeginInvokeOnMainThread(() => {
                Grid.SetRow(EpPickers, (state == 0) ? 1 : 0);

                FadeEpisodes.Scale = (state == 0) ? 1 : 0;
                //episodeView.IsEnabled = state == 0;

                trailerStack.Scale = (state == 2) ? 1 : 0;
                trailerStack.IsEnabled = state == 2;
                trailerStack.IsVisible = state == 2;
                trailerStack.InputTransparent = state != 2;
                // trailerView.HeightRequest = state == 2 ? Math.Min(epView.CurrentTrailers.Count, 4) * 350 : 0;

                EpPickers.IsEnabled = state == 0;
                EpPickers.Scale = state == 0 ? 1 : 0;
                EpPickers.IsVisible = state == 0;

                Recommendations.Scale = state == 1 ? 1 : 0;
                Recommendations.IsVisible = state == 1;
                Recommendations.IsEnabled = state == 1;
                Recommendations.InputTransparent = state != 1;

                SetHeight(state != 0);
                //SetTrailerRec(state == 2);

                if (state == 1) {
                    SetRecs();
                }

            });

            System.Timers.Timer timer = new System.Timers.Timer(10);
            ProgressBar GetBar(int _state)
            {
                return _state switch
                {
                    0 => EPISODESBar,
                    1 => RECOMMENDATIONSBar,
                    2 => TRAILERSBar,
                    _ => null,
                };
            }
            GetBar(prevState).ScaleXTo(0, 70, Easing.Linear);
            GetBar(state).ScaleXTo(1, 70, Easing.Linear);
            timer.Start();
        }

        private void Episodes_Clicked(object sender, EventArgs e)
        {
            ChangedRecState(0);
        }

        private void Recommendations_Clicked(object sender, EventArgs e)
        {
            ChangedRecState(1);
        }

        private void Trailers_Clicked(object sender, EventArgs e)
        {
            ChangedRecState(2);
        }

        #endregion


        private void episodeView_ItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            //  episodeView.Style = TabsStyle.

        }

        private void RecomendationLoaded_SizeChanged(object sender, EventArgs e)
        {

        }

        private void ScrollView_Scrolled(object sender, ScrolledEventArgs e)
        {
            TrailerBtt.TranslationY = -e.ScrollY / 15.0;
            // PlayBttGradient.TranslationY = -e.ScrollY / 15.0;
        }
    }
}

public class MovieResultMainEpisodeView
{
    public ObservableCollection<Trailer> CurrentTrailers { get; set; }

    public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set; get; }
    //public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set { Added?.Invoke(null, null); _MyEpisodeResultCollection = value; } get { return _MyEpisodeResultCollection; } }

    public const int MAX_EPS_PER = 50;
    public EpisodeResult[] AllEpisodes;

    // public event EventHandler Added;

    public MovieResultMainEpisodeView()
    {
        MyEpisodeResultCollection = new ObservableCollection<EpisodeResult>();
        CurrentTrailers = new ObservableCollection<Trailer>();
    }
}





