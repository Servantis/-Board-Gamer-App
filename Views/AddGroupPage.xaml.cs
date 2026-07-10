using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class AddGroupPage : ContentPage
{
    public AddGroupPage(AddGroupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}