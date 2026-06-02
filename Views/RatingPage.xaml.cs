namespace BoardGamerApp.Views;

public partial class RatingPage : ContentPage
{
    public RatingPage()
    {
        InitializeComponent();
        BindingContext = new RatingViewModel();
    }
}
