namespace BoardGamerApp.Views;

public partial class RatingPage : ContentPage
{
    public RatingPage()
    {
        InitializeComponent();

        // Verbindet die XAML-Bindings mit dem RatingViewModel.
        BindingContext = new RatingViewModel();
    }
}
