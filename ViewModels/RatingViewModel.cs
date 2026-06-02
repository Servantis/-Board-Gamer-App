namespace BoardGamerApp;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;

public class RatingViewModel : INotifyPropertyChanged
{
    public ObservableCollection<RatingStar> RatingGastgeberItems { get; }
    public ObservableCollection<RatingStar> RatingEssenItems { get; }
    public ObservableCollection<RatingStar> RatingAbendItems { get; }

    private int _ratingGastgeber;
    public int RatingGastgeber
    {
        get => _ratingGastgeber;
        set
        {
            if (_ratingGastgeber != value)
            {
                _ratingGastgeber = value;
                UpdateStars(RatingGastgeberItems, _ratingGastgeber);
                OnPropertyChanged(nameof(RatingGastgeber));
            }
        }
    }

    private int _ratingEssen;
    public int RatingEssen
    {
        get => _ratingEssen;
        set
        {
            if (_ratingEssen != value)
            {
                _ratingEssen = value;
                UpdateStars(RatingEssenItems, _ratingEssen);
                OnPropertyChanged(nameof(RatingEssen));
            }
        }
    }

    private int _ratingAbend;
    public int RatingAbend
    {
        get => _ratingAbend;
        set
        {
            if (_ratingAbend != value)
            {
                _ratingAbend = value;
                UpdateStars(RatingAbendItems, _ratingAbend);
                OnPropertyChanged(nameof(RatingAbend));
            }
        }
    }

    public RatingViewModel()
    {
        RatingGastgeberItems = CreateRatingCollection(starValue => RatingGastgeber = starValue);
        RatingEssenItems = CreateRatingCollection(starValue => RatingEssen = starValue);
        RatingAbendItems = CreateRatingCollection(starValue => RatingAbend = starValue);
    }

    private ObservableCollection<RatingStar> CreateRatingCollection(Action<int> setRating)
    {
        var list = new ObservableCollection<RatingStar>();

        for (int i = 1; i <= 5; i++)
        {
            int starValue = i;

            list.Add(new RatingStar
            {
                Image = "star_empty.png",
                TapCommand = new Command(() =>
                {
                    Console.WriteLine($"Tapped star {starValue}");
                    setRating(starValue);
                })
            });
        }

        return list;
    }

    private void UpdateStars(ObservableCollection<RatingStar> items, int rating)
    {
        Console.WriteLine($"Rating changed to {rating}");

        for (int i = 0; i < items.Count; i++)
        {
            items[i].Image = (i < rating)
                ? "star_filled.png"
                : "star_empty.png";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

