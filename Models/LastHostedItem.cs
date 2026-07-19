using SQLite;
using System;
using System.Collections.Generic;
using System.Text;


namespace BoardGamerApp.Models;

public class LastHostItem
{
    public string PlayerId { get; set; } = string.Empty;

    public string PlayerName { get; set; } = string.Empty;

    public string HostedDate { get; set; } = string.Empty;

    // Anzeige des ersten Buchstaben des Vornamens
    [Ignore]
    public string Initials =>
        string.IsNullOrWhiteSpace(PlayerName)
            ? "?"
            : PlayerName.Trim()[0]
                .ToString()
                .ToUpperInvariant();



    // Datum-Anzeige Format vorgeben
    [Ignore]
    public string HostedDateDisplay =>
        DateTime.TryParse(HostedDate, out var dt)
            ? dt.ToString("dd.MM.yy")
            : HostedDate;


}
