using BoardGamerApp.ViewModels;

namespace BoardGamerApp.Views;

public partial class GroupPage : ContentPage
{
	public GroupPage()
	{
		InitializeComponent();
        BindingContext = new GroupMembersViewModel();
    }
}