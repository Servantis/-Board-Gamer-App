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
    }
}
