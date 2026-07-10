using BoardGamerApp.ViewModels;
using Microsoft.Maui.Controls;

namespace BoardGamerApp.Views;

[QueryProperty(nameof(GroupId), "groupId")]
public partial class AddPlayerPage : ContentPage
{
    public AddPlayerPage(AddPlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public string GroupId
    {
        set
        {
            if (BindingContext is AddPlayerViewModel vm)
            {
                vm.GroupId = value;
            }
        }
    }
}