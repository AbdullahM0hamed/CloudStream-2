using Android.Content;
using Android.Text;
using Android.Widget;
using CloudStreamForms.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using G = Android.Graphics;

[assembly: ExportRenderer(typeof(SearchBar), typeof(CustomSearchBarRenderer))]
namespace CloudStreamForms.Droid
{
    public class CustomSearchBarRenderer : SearchBarRenderer
    {
        private Context context;
        public CustomSearchBarRenderer(Context context) : base(context)
        {
            this.context = context;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<SearchBar> args)
        {
            base.OnElementChanged(args);
            // Get native control (background set in shared code, but can use SetBackgroundColor here)
            SearchView searchView = (base.Control as SearchView);
            searchView.SetInputType(InputTypes.ClassText | InputTypes.TextVariationNormal);
            if (Settings.TapjackProtectionSearch) {
                searchView.FilterTouchesWhenObscured = true;
            }

            // Access search textview within control
            int textViewId = searchView.Context.Resources.GetIdentifier("android:id/search_src_text", null, null);
            EditText textView = (searchView.FindViewById(textViewId) as EditText);

            if (args.NewElement.FontFamily == null) {
                args.NewElement.FontFamily = CloudStreamForms.Settings.CurrentFont.FontStyle;
            }
            // Set custom colors
            // textView.SetBackgroundColor(G.Color.Rgb(25, 25, 25));
            int color = Settings.BlackColor - 5;
            if (color < 0) color = -color * 2;
            textView.SetBackgroundColor(G.Color.Rgb(color, color, color));//Settings.BlackBg ? G.Color.Rgb(12, 12, 12) : G.Color.Rgb(25, 25, 25));

            textView.SetHintTextColor(G.Color.Rgb(64, 0, 0));
            textView.SetTextColor(G.Color.Rgb(200, 0, 0));
            /*
            textView.SetTextColor(G.Color.Rgb(32, 32, 32));
            textView.SetHintTextColor(G.Color.Rgb(128, 128, 128));*/


            // Customize frame color
            int frameId = searchView.Context.Resources.GetIdentifier("android:id/search_plate", null, null);
            Android.Views.View frameView = (searchView.FindViewById(frameId) as Android.Views.View);
            frameView.SetBackgroundColor(G.Color.Transparent);//G.Color.ParseColor(CloudStreamForms.Settings.MainBackgroundColor));
            

            var searchIconId = searchView.Resources.GetIdentifier("android:id/search_mag_icon", null, null);
            if (searchIconId > 0) {
                var searchPlateIcon = searchView.FindViewById(searchIconId);
                searchPlateIcon.ScaleX = 0.75f;
                searchPlateIcon.ScaleY = 0.75f;
                searchPlateIcon.TranslationX = -5;
                
                (searchPlateIcon as ImageView).SetImageDrawable(context.GetDrawable(Resource.Drawable.MainSearchIcon));
                (searchPlateIcon as ImageView).SetColorFilter(G.Color.Rgb(190, 190, 190));
            }
        }
    }
}
