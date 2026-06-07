using BoardGamerApp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoardGamerApp.Services.Interfaces
{
    public interface IHostScheduleService
    {
        void EnsureHost(List<GroupMember> members);
    }
}
