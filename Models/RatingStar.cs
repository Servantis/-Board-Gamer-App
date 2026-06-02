namespace BoardGamerApp;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

public class RatingStar : INotifyPropertyChanged
{
    private string? _image;
    public string? Image
    {
        get => _image;
        set
        {
            if (_image != value)
            {
                _image = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand? TapCommand { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
