using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using SQLite;

namespace BoardGamerApp.Models;

[Table("group_members")]
public class GroupMember : BaseSyncEntity
{
    public class GroupMember: INotifyPropertyChanged
    {
        private bool _isNextHost;
        private bool _hostedFlag;

        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

    [Indexed(Name = "ux_group_members_group_player", Order = 2, Unique = true)]
    [NotNull]
    public string PlayerId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public bool HostedFlag
        {
            get => _hostedFlag;
            set
            {
                _hostedFlag = value;
                OnPropertyChanged();
            }
        }

        public bool IsNextHost
        {
            get => _isNextHost;
            set
            {
                _isNextHost = value;
                OnPropertyChanged();
            }
        }

    [NotNull]
    public string Status { get; set; } = BoardGamerConstants.GroupMemberStatus.Active;
        public DateTime LastHostedDate { get; set; }
        public string LastHostedDateFormatted => 
            LastHostedDate.ToString("dd.MM.yy");
        public string LastHostedDisplay =>
            $"zuletzt: {LastHostedDateFormatted}";

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return string.Empty;
         
                var initial = !string.IsNullOrWhiteSpace(LastName)
                    ? $"{LastName[0]}."
                    : "";

                return $"{Name} {initial}".Trim();
            }
        }

        // Initialien aus erstem Buchstaben des Vor- und Nachnamens bilden
        public string Initials =>
            string.Concat(
                string.IsNullOrWhiteSpace(Name) ? "" : Name.Trim()[0].ToString(),
                string.IsNullOrWhiteSpace(LastName) ? "" : LastName.Trim()[0].ToString()
            ).ToUpper();

        // Update die Flag sobald sich diese ändert
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
