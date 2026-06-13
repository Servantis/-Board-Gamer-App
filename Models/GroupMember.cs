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
    [Table("players")]
    public class GroupMember: INotifyPropertyChanged
    {
        private bool _isNextHost;
        private bool _hostedFlag;
        private DateTime _lastHostedDate;

        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("last_name")]
        public string LastName { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("hosted_flag")]
        public bool HostedFlag
        {
            get => _hostedFlag;
            set
            {
                _hostedFlag = value;
                OnPropertyChanged();
            }
        }

        [Column("is_next_host")]
        public bool IsNextHost
        {
            get => _isNextHost;
            set
            {
                _isNextHost = value;
                OnPropertyChanged();
            }
        }

        [Column("last_hosted_date")]
        public DateTime LastHostedDate
        {
            get => _lastHostedDate;
            set
            {
                _lastHostedDate = value;
                OnPropertyChanged();
            }
        }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("rotation_order")]
        public int RotationOrder { get; set; }

        // Anzeige Format Name + erster Buchstabe des Nachnamens.
        [Ignore]
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
        [Ignore]
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
