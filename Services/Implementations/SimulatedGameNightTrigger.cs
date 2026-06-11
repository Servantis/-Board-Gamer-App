using BoardGamerApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoardGamerApp.Services.Implementations
{
    public class SimulatedGameNightTrigger: IGameNightTrigger
    {
        private DateTime _fixedEndTime = DateTime.Now.AddMinutes(1);

        /*  public bool IsGameNightOver()
          {
              return DateTime.Now >= _fixedEndTime;
          } */

        // testweise auf true, damit Logik nach Eventtrigger getestet werden kann
        public bool IsGameNightOver()
        {
            return true;
        }
    }
}
