using CloudStreamForms.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Search : ContentPage
    {
        public ObservableCollection<SearchResult> mySearchResultCollection;
        public static Poster mainPoster;
        ListView listView;
        public string startText = "";

        protected override void OnAppearing()
        {
            base.OnAppearing();
            BackgroundColor = Settings.BlackRBGColor;
        }
        public Search()
        {
            InitializeComponent();
            BackgroundColor = Settings.BlackRBGColor;

            mainCore.searchLoaded += Search_searchLoaded;

            //BindingContext = new SearchPageViewer();

            mySearchResultCollection = new ObservableCollection<SearchResult>() {
            };

            SearchBar searchBar = new SearchBar() {
                Placeholder = "Movie Search...",
                CancelButtonColor = Color.FromRgb(190, 190, 190),
                TranslationY = 3,
            };

            searchBar.TextChanged += SearchBar_TextChanged;
            searchBar.SearchButtonPressed += SearchBar_SearchButtonPressed;
            if (Device.RuntimePlatform == Device.Android) {
                /*
                searchBar.TextColor = Color.FromHex(MainPage.primaryColor);
                searchBar.PlaceholderColor = Color.FromHex(MainPage.primaryColor);
                searchBar.CancelButtonColor = Color.FromHex(MainPage.primaryColor);
                */
            }
            listView = new ListView {
                // Source of data items.
                ItemsSource = mySearchResultCollection,
                RowHeight = 50,

                // Define template for displaying each item.
                // (Argument of DataTemplate constructor is called for 
                //      each item; it must return a Cell derivative.)
                ItemTemplate = new DataTemplate(() => {
                    // Create views with bindings for displaying each property.

                    Label nameLabel = new Label();
                    Label desLabel = new Label();
                    // Image poster = new Image();
                    nameLabel.SetBinding(Label.TextProperty, "Title");
                    desLabel.SetBinding(Label.TextProperty, "Extra");
                    // poster.SetBinding(Image.SourceProperty, "Poster");
                    //  desLabel.FontSize = nameLabel.FontSize / 1.2f;
                    desLabel.FontSize = 12;
                    desLabel.TextColor = Color.FromHex("#828282"); // 
                    nameLabel.TextColor = Color.FromHex("#ff0000");
                    nameLabel.FontSize = 15;

                    desLabel.TranslationX = 10;
                    nameLabel.TranslationX = 10;



                    //nameLabel.SetBinding(Label.d, "Extra");
                    /*
                    Label birthdayLabel = new Label();
                    birthdayLabel.SetBinding(Label.TextProperty,
                        new Binding("Birthday", BindingMode.OneWay,
                            null, null, "Born {0:d}"));

                    BoxView boxView = new BoxView();
                    boxView.SetBinding(BoxView.ColorProperty, "FavoriteColor");*/

                    // Return an assembled ViewCell.
                    return new ViewCell {
                        View = new StackLayout {
                            Padding = new Thickness(0, 9),
                            Orientation = StackOrientation.Horizontal,
                            Children =
                            {                                  

                                    //boxView,
                                    new StackLayout
                                    {
                                        VerticalOptions = LayoutOptions.CenterAndExpand,
                                        Spacing = 0,
                                        Children =
                                        {
                                        //    poster,
                                            nameLabel,
                                            desLabel,
                                            //birthdayLabel
                                        }
                                        }

                                }
                        }
                    };
                })
            };
            listView.ItemTapped += ListView_ItemTapped;
            listView.SeparatorColor = Color.Transparent;
            listView.VerticalScrollBarVisibility = Settings.ScrollBarVisibility;

            // Accomodate iPhone status bar.
            // this.Padding = new Thickness(10, Device.OnPlatform(20, 0, 0), 10, 5);

            // Build the page.
            this.Content = new StackLayout {
                Children =
                {
                    searchBar,
                    new Image() {Source = App.GetImageSource("gradient.png"), HeightRequest=3,HorizontalOptions=LayoutOptions.Fill, ScaleX=100,Opacity=0.3},
                    listView
        //new BoxView() {Color = Color.LightGray,HeightRequest=1,TranslationY=-2 ,}, // {Color = new Color(   .188, .247, .624) { },HeightRequest=2 },
                }
            };
            // searchBar.Text = startText;
            // print(">>" + startText);
            //searchBar.Focus();
            // print(MainSearchResultList.ItemsSource.ToString()  + "<<<<<<<<<");

        }

        private void SearchBar_SearchButtonPressed(object sender, EventArgs e)
        {
            mainCore.QuickSearch(((SearchBar)sender).Text);
        }

        private void ListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            print(e.ItemIndex);
            print(activePosters[e.ItemIndex].name + "<<<");
            listView.SelectedItem = null;
            PushPage(activePosters[e.ItemIndex], Navigation);
        }

        protected override bool OnBackButtonPressed()
        {
            return base.OnBackButtonPressed(); //   return true; // 
        }


        public static async void PushPage(Poster _mainPoster, INavigation navigation)
        {
            if (mainPoster.url == _mainPoster.url) return;

            mainPoster = _mainPoster;
            Page p = new MovieResult();// { mainPoster = mainPoster };
            try {
                await navigation.PushModalAsync(p, false);

            }
            catch (Exception) {

            }
        }

        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Settings.SearchEveryCharEnabled) {
                mainCore.QuickSearch(e.NewTextValue);
            }
        }

        List<Poster> activePosters = new List<Poster>();

        private void Search_searchLoaded(object sender, List<Poster> e)
        {
            activePosters = e;
            Device.BeginInvokeOnMainThread(() => { 
                mySearchResultCollection.Clear();
                for (int i = 0; i < mainCore.activeSearchResults.Count; i++) {
                    string extra = mainCore.activeSearchResults[i].extra;
                    if (extra != "") {
                        extra = " - " + extra;
                    }
                    mySearchResultCollection.Add(new SearchResult() { Id = i, Title = mainCore.activeSearchResults[i].name + extra, Extra = mainCore.activeSearchResults[i].year, Poster = mainCore.activeSearchResults[i].posterUrl });
                }
            });
        }
    }
}