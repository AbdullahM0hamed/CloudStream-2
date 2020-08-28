using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using static CloudStreamForms.Core.CloudStreamCore;
using CloudStreamForms.Core;
using CloudStreamForms.Pages;
using Rg.Plugins.Popup.Services;
using System.Linq;

namespace CloudStreamForms
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer

    [DesignTimeVisible(false)]
    public partial class MainPage : Xamarin.Forms.TabbedPage
    {
        public const string DARK_BLUE_COLOR = "#303F9F";
        public const string LIGHT_BLUE_COLOR = "#829eff";
        public const string LIGHT_BLACK_COLOR = "#595959";
        public const string LIGHT_LIGHT_BLACK_COLOR = "#e6e6e6";
        public const string BLACK_COLOR = "#111111";
        public const string ITEM_COLOR = "#617eff";
        public const string LIGHT_DARK_BLUE_COLOR = "#1976D2";

        public const bool IS_EMTY_BUILD = false;
        public const bool IS_TEST_VIDEO = false;

        public static string intentData = "";
        public static MainPage mainPage;

        [Serializable]
        public struct BookmarkPoster
        {
            public string name;
            public string posterUrl;
            public string id;
            //  public Button button;
        }

        public static bool NewGithubUpdate {
            get {
                if (githubUpdateTag == "") { return false; }
                else { return ("v" + App.GetBuildNumber() != githubUpdateTag); }
            }
        }

        public static string githubUpdateTag = "";
        public static string githubUpdateText = "";

        public static async Task ShowUpdate(bool skipIntro = false)
        {
            if (Device.RuntimePlatform == Device.Android) {
                var recommendedArc = (App.AndroidVersionArchitecture)App.PlatformDep.GetArchitecture();

                string option = skipIntro ? "Download Update" : await ActionPopup.DisplayActionSheet($"App update {githubUpdateTag}", "Download Update", "Ignore this time", "Dont show this again");
                if (option == "Download Update") {

                    List<string> options = new List<string>() { };
                    var arcs = App.GetEnumList<App.AndroidVersionArchitecture>();
                    foreach (var ver in arcs) {
                        options.Add(App.GetVersionPublicName(ver) + (recommendedArc == ver ? " (Recommended)" : " "));
                    }
                    string version = await ActionPopup.DisplayActionSheet("Download App Architecture", options.ToArray());
                    if (version == "Cancel" || version == "") return;
                    foreach (var ver in arcs) {
                        if (version.StartsWith(App.GetVersionPublicName(ver) + " ")) {
                            App.DownloadNewGithubUpdate(githubUpdateTag, ver);
                            break;
                        }
                    }
                }
                else if (option == "Dont show this again") {
                    Settings.ShowAppUpdate = false;
                }
            }
        }

        public static void CheckGitHubUpdate()
        {
            try {
                if (Device.RuntimePlatform == Device.Android) { // ONLY ANDROID CAN UPDATE

                    TempThread tempThred = mainCore.CreateThread(4);
                    mainCore.StartThread("GitHub Update Thread", async () => {
                        try {
                            string d = mainCore.DownloadString("https://github.com/LagradOst/CloudStream-2/releases", tempThred);
                            if (!mainCore.GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                            string look = "/LagradOst/CloudStream-2/releases/tag/";
                            //   float bigf = -1;
                            //     string bigUpdTxt = "";
                            // while (d.Contains(look)) {

                            githubUpdateTag = FindHTML(d, look, "\"");
                            githubUpdateText = FindHTML(d, look + githubUpdateTag + "\">", "<");
                            if (Settings.ShowAppUpdate && NewGithubUpdate) {
                                await ShowUpdate();
                            }
                            print("UPDATE SEARCHED: " + githubUpdateTag + "|" + githubUpdateText);
                        }
                        finally {
                            mainCore.JoinThred(tempThred);
                        }
                    });
                }
            }
            catch (Exception _ex) {
                print("Github ex::" + _ex);
            }

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.isOnMainPage = true;
            App.UpdateBackground();
            // App.Test();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            App.isOnMainPage = false;
            App.UpdateBackground();
        }

        // public static readonly string[] baseIcons = new string[] { "outline_home_white_48dp.png", "searchIcon.png", "outline_get_app_white_48dp.png", "outline_settings_white_48dp.png" };
        public static readonly string[] baseIcons = new string[] { "outline_home_white_48dp.png", "MainSearchIcon.png", "MainNetflixDownloadIcon.png", "MainSettingsIcon_backup.png" };
        public static readonly string[] onSelectedIcons = new string[] { "outline_home_white_48dp.png", "MainSearchIcon.png", "MainNetflixDownloadIcon.png", "MainSettingsIcon_backup.png" };
        //    public static readonly string[] onSelectedIcons = new string[] { "sharp_home_white_48dp.png", "searchIcon.png", "sharp_get_app_white_48dp.png", "sharp_settings_white_48dp.png" };

        public static void OnIconStart(int i)
        {
            mainPage.Children[i].IconImageSource = onSelectedIcons[i];
        }

        public static void OnIconEnd(int i)
        {
            mainPage.Children[i].IconImageSource = baseIcons[i];
        }

        public static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            print(" ===================================== SUPERFATAL EX =====================================\n" +
                e.ExceptionObject.ToString() + "\n" +
                e.IsTerminating + "\n" +
                sender.ToString() +
                "\n ===================================== END ====================================="
                );
        }

        public async void LoadVideoPage()
        {
            await Device.InvokeOnMainThreadAsync(async () => {
                await Navigation.PushModalAsync(new VideoPage(VideoPage.lastVideo), true);
            });
        }

        public MainPage()
        {
            InitializeComponent();

            App.OnAppReopen += async (o, e) => {
                if (VideoPage.CanReopen) {
                    VideoPage.CanReopen = false;
                    LoadVideoPage();
                }
            };

            App.OnAppNotInForground += (o, e) => {
                App.SaveData();
            };

            mainPage = this; CloudStreamCore.mainPage = mainPage;

            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            //Application. += MYThreadHandler;
            //throw new Exception("Kaboom");

            if (IS_TEST_VIDEO) {
                Page p = new VideoPage(new VideoPage.PlayVideo() { descript = "", name = "Black Bunny", episode = -1, season = -1, MirrorNames = new List<string>() { "Googlevid" }, MirrorUrls = new List<string>() { "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4" } });//new List<string>() { "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4" }, new List<string>() { "Black" }, new List<string>() { });// { mainPoster = mainPoster };
                mainPage.Navigation.PushModalAsync(p, false);
                print("PUST: ::: :");
            }

            // Page _p = new ChromeCastPage();// { mainPoster = mainPoster };
            // Navigation.PushModalAsync(_p, false);
            // print("TEXTFILE:\n" + CloudStreamForms.Script.SyncWrapper.GenerateTextFile());
            // Script.SyncWrapper.SetKeysFromTextFile(Script.SyncWrapper.GenerateTextFile());

            if (IS_EMTY_BUILD) return;


            List<string> names = new List<string>() { "Home", "Search", "Downloads", "Settings" };
            //List<string> icons = new List<string>() { "homeIcon.png", "searchIcon.png", "downloadIcon.png", "baseline_settings_applications_white_48dp_inverted_big.png" };
            List<Page> pages = new List<Page>() { new Home(), new Search(), new Download(), new SettingsPage(), };

            for (int i = 0; i < pages.Count; i++) {
                Children.Add(pages[i]);
                Children[i].Title = names[i];
                Children[i].IconImageSource = baseIcons[i];
            }
            On<Xamarin.Forms.PlatformConfiguration.Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);
            LateCheck();

            /*
            if (Settings.IS_TEST_BUILD) {
                return;
            }
            try {
                OnIconStart(0);

                int oldPage = 0;
                CurrentPageChanged += (o, e) => {
                    try {
                        OnIconEnd(oldPage);
                        for (int i = 0; i < pages.Count; i++) {
                            if ((pages[i].GetType()) == CurrentPage.GetType()) {
                                OnIconStart(i);
                                oldPage = i;
                            }
                        }
                    }
                    catch (Exception _ex) {
                        error(_ex);
                    }
                };
            }
            catch (Exception _ex) {
                error(_ex);
            }*/
            // BarBackgroundColor = Color.Black;
            //   BarTextColor = Color.OrangeRed;

            //Page _p = new ReviewPage("tt0371746", "Iron Man");
            //MainPage.mainPage.Navigation.PushModalAsync(_p);

            //PushPageFromUrlAndName("tt4869896", "Overlord");
            //  PushPageFromUrlAndName("tt0409591", "Naruto");
            //  PushPageFromUrlAndName("tt10885406", "Ascendance of a Bookworm");
            // PushPageFromUrlAndName("tt9054364", "That Time I Got Reincarnated as a Slime");
            // PushPageFromUrlAndName("tt0371746", "Iron Man");
            // PushPageFromUrlAndName("tt10954274", "ID: Invaded");
        }


        async void LateCheck()
        {
            await Task.Delay(5000);
         //   App.PlatformDep.PictureInPicture();
            try {
                CheckGitHubUpdate();
                MainChrome.StartImageChanger();
                MainChrome.GetAllChromeDevices();
            }
            catch (Exception _ex) {
                error("ERROR IN LATECHECK::: " + _ex);
            }
        }

        public static void PushPageFromUrlAndName(string url, string name)
        {
            try {
                Poster _p = new Poster() { url = url, name = name };
                Search.PushPage(_p, MainPage.mainPage.Navigation);
            }
            catch (Exception) {

            }
        }

        public static void PushPageFromUrlAndName(string intentData)
        {
            string url = FindHTML(intentData, "cloudstreamforms:", "Name=");
            string name = FindHTML(intentData, "Name=", "=EndAll");
            print("MUSHFROMMMM: " + name + "|" + url);
            //Task.Delay(10000);
            if (name != "" && url != "") {
                PushPageFromUrlAndName(url, System.Web.HttpUtility.UrlDecode(name));
            }
        }        /// <summary>
                 /// Creates color with corrected brightness.
                 /// </summary>
                 /// <param name="color">Color to correct.</param>
                 /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
                 /// Negative values produce darker colors.</param>
                 /// <returns>
                 /// Corrected <see cref="Color"/> structure.
                 /// </returns>
        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (correctionFactor < 0) {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromRgba((int)red, (int)green, (int)blue, color.A);
        }
    }


    // -------------------------------- END CHROMECAST --------------------------------


}

// =============================================================== NOT IMPLEMENTED PROVIDERS (COMING SOON) ===============================================================



// =============================================================== watch.animeonline360 ===============================================================

/*
            string url = "https://watch.animeonline360.com/?s=overlord";

            string d = DownloadString(url);
            string lookFor = " class=\"title\"> <a href=\"";
            while (d.Contains(lookFor)) {
                string uri = FindHTML(d, lookFor, "\"");
                d = RemoveOne(d, lookFor);
                string name = FindHTML(d, "/\">", "<",decodeToNonHtml:true);
                print(name + "|" + uri);
            }
            */
/*
string url = "https://watch.animeonline360.com/tvshows/overlord-dubbed/";
string d = DownloadString(url);
List<string> episodes = new List<string>();
string lookFor = "div class=\'numerando\'>";
while (d.Contains(lookFor)) {
    string uri = FindHTML(d, "<a href=\'", "\'");
    d = RemoveOne(d, lookFor);
    episodes.Add(uri);
}*/

/*
string url = "https://watch.animeonline360.com/episodes/air-episode-1-english-dubbed/";
string d = DownloadString(url);
string id = FindHTML(d, "data-post=\'", "\'");
string _d = PostRequest("https://watch.animeonline360.com/wp-admin/admin-ajax.php", url, "action=doo_player_ajax&post=" + id + "&nume=1&type=tv");
string source = FindHTML(_d, "src=\'", "\'");
 d = DownloadString(source);
string videoUrl = FindHTML(d, "source src=\"", "\"");
print(videoUrl);*/



// =============================================================== twist.moe ===============================================================

/*
        // =============== GET COOKIE SITE ===============
        string d = GetHTML("https://twist.moe/a/angel-beats/1");
        string code = FindHTML(d, "<script>", "</script>").Replace("e(r);", "alert(r);");
        string token = "";
        string token2 = "";
        string tokenCook = "";
        var engine = new Engine().SetValue("alert", new Action<string>((a) => { token = a; }));//.SetValue("log", new Action<string>((a) => { token2 = a; })); ;
        engine.Execute(code);
        token = token.Replace("location.reload();", "alert(e); alertCook(doccookie);");
        string find = "+ \';path=";
        token = token.Replace(find + "" + FindHTML(token, find, "\'") + "\'", "").Replace("document.cookie=", "var doccookie=");

        var engine2 = new Engine()
                 .SetValue("alert", new Action<string>((a) => { token2 = a; })).SetValue("alertCook", new Action<string>((a) => { tokenCook = a; }));
        engine2.Execute(token);

        //string _d = HTMLGet("https://twist.moe/api/anime/angel-beats/sources", "https://twist.moe/a/angel-beats/1", cookies: new List<Cookie>() { new Cookie() { Name = FindHTML("|" + tokenCook, "|", "="), Value = FindHTML(tokenCook + "|", "=", "|"), Expires = DateTime.Now.AddSeconds(1000) } }, keys: new List<string>() { "x-access-token" }, values: new List<string>() { token2 }) ;

        // =============== GET REAL SITE ===============

        string _d = HTMLGet("https://twist.moe/a/angel-beats/1", "https://twist.moe/a/angel-beats/1", cookies: new List<Cookie>() { new Cookie() { Name = FindHTML("|" + tokenCook, "|", "="), Value = FindHTML(tokenCook + "|", "=", "|"), Expires = DateTime.Now.AddSeconds(1000) } });
        string openToken = "";
        string lookFor = "<link href=\"/_nuxt/";
        while (_d.Contains(lookFor)) {
            if (openToken == "") {
                string ___d = FindHTML(_d, lookFor, "\"");
                if (___d.EndsWith(".js")) {
                    string dKey = DownloadString("https://twist.moe/_nuxt/" + ___d);
                    openToken = FindHTML(dKey, "x-access-token\":\"", "\"");
                    //x-access-token":"
                }
            }
            _d = RemoveOne(_d, lookFor);
        }
        // =============== GET SOURCES ===============
        string seriesD = HTMLGet("https://twist.moe/api/anime/angel-beats/sources", "https://twist.moe/a/angel-beats/1", cookies: new List<Cookie>() { new Cookie() { Name = FindHTML("|" + tokenCook, "|", "="), Value = FindHTML(tokenCook + "|", "=", "|"), Expires = DateTime.Now.AddSeconds(1000) } }, keys: new List<string>() { "x-access-token" }, values: new List<string>() { openToken }) ;
        print(seriesD);
        string fetch = FetchMoeUrlFromSalted("U2FsdGVkX19keEYdmWm5SfhOhvOubne48rkyrP/vvFp/9yurDNMI20rUCXnlkf8DFLW9bSJfKLFkKsN6P7Hhy+zYjYdqjOB/EyNH9+gdhdAjUJVKBbdui/Kk3Avx84/e");
        print(fetch);*/

// =============================================================== twist.moe LINK FETCH ===============================================================


/*        static string FetchMoeUrlFromSalted(string _salted)
    {
        byte[] CreateMD5Byte(byte[] input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] hashBytes = md5.ComputeHash(input);
                return hashBytes;
            }
        }
        string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            string plaintext = null;
            using (Aes aesAlg = Aes.Create()) {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(cipherText)) {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8)) {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }
        byte[] SubArray(byte[] data, int index, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
        byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays) {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
        byte[] bytes_to_key(byte[] data, byte[] _salt, int output = 48)
        {
            data = Combine(data, _salt);
            byte[] _key = CreateMD5Byte(data);
            List<byte> final_key = _key.ToList();
            while (final_key.Count < output) {
                _key = CreateMD5Byte(Combine(_key, data));
                final_key.AddRange(_key);
            }
            return SubArray(final_key.ToArray(), 0, output);
        }

        const string KEY = "LXgIVP&PorO68Rq7dTx8N^lP!Fa5sGJ^*XK";
        var f = System.Convert.FromBase64String(_salted);
        var salt = SubArray(f, 8, 8);
        var bytes = System.Text.Encoding.ASCII.GetBytes(KEY);
        byte[] key_iv = bytes_to_key(bytes, salt, 32 + 16);
        byte[] key = SubArray(key_iv, 0, 32);

        byte[] iv = SubArray(key_iv, 32, 16);
        return FindHTML(DecryptStringFromBytes_Aes(SubArray(f, 16, f.Length - 16), key, iv) + "|", "/", "|").Replace(" ", "%20");
    }
*/
