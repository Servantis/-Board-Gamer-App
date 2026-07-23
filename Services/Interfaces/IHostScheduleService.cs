using BoardGamerApp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoardGamerApp.Services.Interfaces
{
    public interface IHostScheduleService
    {
        Task ProcessHostChangeAsync(string groupId);
        Task EnsureNextHostExistsAsync(string groupId);

        /// <summary>
        /// Legt falls nötig automatisch einen Folgetermin an, nachdem ein Termin
        /// abgeschlossen ("completed") und der Gastgeber-Wechsel verarbeitet wurde
        /// (siehe HostScheduleService für Details). Muss NACH ProcessHostChangeAsync
        /// aufgerufen werden, damit der neue Gastgeber (IsNextHost) schon feststeht.
        /// </summary>
        Task CreateFollowUpGameNightIfNeededAsync(string groupId);
    }
}
