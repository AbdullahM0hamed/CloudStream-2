using CloudStreamForms.Core;
using CloudStreamForms.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DownloadViewPage : ContentPage
    {
        public int currentId = 0;
        //   public MainDownloadEpisodeView epView;
        private ObservableCollection<EpisodeResult> _MyEpisodeResultCollection;
        public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set { Added?.Invoke(null, null); _MyEpisodeResultCollection = value; } get { return _MyEpisodeResultCollection; } }

        public event EventHandler Added;

        private bool _isRefreshing = false;
        public bool IsRefreshing {
            get { return _isRefreshing; }
            set {
                _isRefreshing = value;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }

        public ICommand RefreshCommand {
            get {
                return new Command(async () => {
                    IsRefreshing = true;
                    print("YEET;;;;dd");
                    UpdateEpisodes();
                    await Task.Delay(100);
                    // await RefreshData();

                    IsRefreshing = false;
                });
            }
        }


        public DownloadViewPage(int id)
        {
            InitializeComponent();
            currentId = id;
            //   epView = new MainDownloadEpisodeView();
            MyEpisodeResultCollection = new ObservableCollection<EpisodeResult>();
            //  BindingContext = epView;
            BindingContext = this;
            BackgroundColor = Settings.BlackRBGColor;
        }

        void SetHeight()
        {
            Device.BeginInvokeOnMainThread(() => episodeView.HeightRequest = MyEpisodeResultCollection.Count * episodeView.RowHeight + 20);//episodeView.HeightRequest = MyEpisodeResultCollection.Count * episodeView.RowHeight + 20);
        }

        private void ViewCell_Tapped(object sender, EventArgs e)
        {
            EpisodeResult episodeResult = (EpisodeResult)(((ViewCell)sender).BindingContext);
            HandleEpisode(episodeResult);
            episodeView.SelectedItem = null;
        }

        async Task HandleEpisode(EpisodeResult episodeResult)
        {
            await Download.HandleEpisodeTapped(episodeResult.Id, this);
            UpdateEpisodes();
        }

        /*
        private void ImageButton_Clicked(object sender, EventArgs e)
        {
            EpisodeResult episodeResult = ((EpisodeResult)((ImageButton)sender).BindingContext);
            HandleEpisode(episodeResult, this);
            //PlayEpisode(episodeResult);
        }*/

        private void ChromeCastBtt_Clicked(object sender, EventArgs e)
        {
            WaitChangeChromeCast();
        }

        async void WaitChangeChromeCast()
        {
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

        private void OpenChromecastView(object sender, EventArgs e)
        {
            if (sender != null) {
                ChromeCastPage.isActive = false;
            }
            if (!ChromeCastPage.isActive) {
                OpenChrome(false);
            }
        }

        public static void OpenChrome(bool validate = true)
        {
            if (!ChromeCastPage.isActive && Download.chromeDownload != null) {
                Page p = ChromeCastPage.CreateChromePage(Download.chromeDownload);// new (chromeResult, chromeMovieResult); //{ episodeResult = chromeResult, chromeMovieResult = chromeMovieResult };
                MainPage.mainPage.Navigation.PushModalAsync(p, false);
            }
        }



        void UpdateEpisodes()
        {
            MyEpisodeResultCollection.Clear();
            try {
                var header = Download.downloadHeaders[currentId];
                var helper = Download.downloadHelper[currentId];
                // App.GetDownloadHeaderInfo(currentId);
                List<EpisodeResult> activeEpisodes = new List<EpisodeResult>();

                foreach (var key in helper.infoIds) {
                    var info = App.GetDownloadInfo(key);//Download.downloads[key];
                    if (info != null) {
                        Download.downloads[key] = info;
                        if (info.state.totalBytes == 0 && info.state.bytesDownloaded != 1) {
                            print("DC 0 DATAA:::: ");
                            Download.RemoveDownloadCookie(key);
                        }
                        else {
                            int ep = info.info.episode;
                            int ss = info.info.season;

                            string fileUrl = info.info.fileUrl;
                            string fileName = info.info.name;

                            print(info.state.bytesDownloaded + "|" + info.state.totalBytes);
                            int dloaded = (int)info.state.ProcentageDownloaded;
                            // string extra = (info.state.state == App.DownloadState.Downloaded ? "" : App.ConvertBytesToAny(info.state.bytesDownloaded, 0, 2) + " MB of " + App.ConvertBytesToAny(info.state.totalBytes, 0, 2) + " MB"); 
                            string extra = $" {dloaded }%";
                            print("EXTRA: " + extra + "|" + dloaded);
                            //.TapCom = new Command(async (s) => {
                            long pos;
                            long len;
                            double _progress = 0;
                            if ((pos = App.GetViewPos(info.info.id)) > 0) {
                                if ((len = App.GetViewDur(info.info.id)) > 0) {
                                    _progress = (double)pos / (double)len;
                                    print("SET PROGRES:::" + "|" + _progress);
                                }
                            }

                            activeEpisodes.Add(new EpisodeResult() {
                                OgTitle = info.info.name,
                                ExtraDescription = $"{Download.GetExtraString(info.state.state)}{((info.state.state == App.DownloadState.Downloaded || dloaded == -1) ? "" : extra)}",
                                Title = (ep != -1 ? $"S{ss}:E{ep} " : "") + info.info.name,
                                Description = info.info.description,
                                Episode = ep,
                                Season = ss,
                                Id = info.info.id,
                                PosterUrl = info.info.hdPosterUrl,
                                Progress = _progress,
                                TapCom = new Command((s) => {
                                    if (info.info.dtype == App.DownloadType.Normal) MovieResult.SetEpisode("tt" + info.info.id);
                                    Download.PlayDownloadedFile(info);
                                    //Download.PlayDownloadedFile(fileUrl, fileName, info.info.episode, info.info.season, info.info.episodeIMDBId, info.info.source);
                                    // Download.PlayVLCFile(fileUrl, fileName, info.info.id.ToString()); 
                                }),
                                DownloadPlayBttSource = App.GetImageSource("nexflixPlayBtt.png")
                            });
                        }
                    }
                }

                activeEpisodes = activeEpisodes.OrderBy(t => (t.Episode + t.Season * 1000)).ToList();
                for (int i = 0; i < activeEpisodes.Count; i++) {
                    MyEpisodeResultCollection.Add(activeEpisodes[i]);
                }
            }
            catch (Exception _ex) {
                print("EXUpdateDEpisodes::: " + _ex);
            }
            SetHeight();

            if (MyEpisodeResultCollection.Count == 0) {
                Navigation.PopModalAsync();
            }
        }

        public void ForceUpdateAppearing(object s, EventArgs e)
        {
            UpdateEpisodes();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MainChrome.OnChromeImageChanged -= MainChrome_OnChromeImageChanged;
            App.ForceUpdateVideo -= ForceUpdateAppearing;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateEpisodes();

            ImgChromeCastBtt.Source = App.GetImageSource(MainChrome.CurrentImageSource);
          
            UpdateVisual();
            MainChrome.OnChromeImageChanged += MainChrome_OnChromeImageChanged;
             
            print("CONTAINS::: " + MainChrome.IsChromeDevicesOnNetwork);
            void ChromeUpdate()
            {
                ChromeHolder.IsVisible = MainChrome.IsChromeDevicesOnNetwork;
                ChromeHolder.IsEnabled = ChromeHolder.IsVisible;
            }

            ChromeUpdate();
            MainChrome.OnChromeDevicesFound += (o, e) => {
                ChromeUpdate();
            };

           // MainChrome.GetAllChromeDevices();

            App.ForceUpdateVideo += ForceUpdateAppearing;

            if (Device.RuntimePlatform == Device.UWP) {
                OffBar.IsVisible = false;
                OffBar.IsEnabled = false;
                //  DownloadSizeGrid.HeightRequest = 25;
            }
            else {
                OffBar.Source = App.GetImageSource("gradient.png"); OffBar.HeightRequest = 3; OffBar.HorizontalOptions = LayoutOptions.Fill; OffBar.ScaleX = 100; OffBar.Opacity = 0.3; OffBar.TranslationY = 9;
            }
        }
        void UpdateVisual()
        {
            if (MainChrome.IsConnectedToChromeDevice) {
                ChromeName.Text = "Connected to " + MainChrome.chromeRecivever.FriendlyName;
            }
            else {
                ChromeName.Text = "Not connected";
            }
            ChromeName.TextColor = MainChrome.CurrentImage > 0 ? Color.FromHex(MainPage.LIGHT_BLUE_COLOR) : Color.FromHex("#ff0000");
        }
        private void MainChrome_OnChromeImageChanged(object sender, string e)
        {
            ImgChromeCastBtt.Source = App.GetImageSource(e);
            UpdateVisual();
        }
    }


}