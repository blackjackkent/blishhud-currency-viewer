// Adapted from https://github.com/a727891/BlishHud-Raid-Clears/blob/main/BlishHud-Raid-Clears/Features/Shared/Services/ApiPollService.cs

using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using System;

namespace BlishHudCurrencyViewer.Services
{
    public class ApiPollingService : IDisposable
    {
        private int _refreshIntervalMilliseconds;
        private double _runningTimer;
        public event EventHandler<bool> ApiPollingTrigger;

        public ApiPollingService(int refreshIntervalMilliseconds = 300000)
        {
            _refreshIntervalMilliseconds = refreshIntervalMilliseconds;
        }

        public void Dispose()
        {
        }

        public void Update(GameTime gameTime)
        {
            _runningTimer += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (_runningTimer >= _refreshIntervalMilliseconds)
            {
                ApiPollingTrigger?.Invoke(this, true);
                _runningTimer = 0;
            }
        }

        public void Invoke()
        {
            _runningTimer = 0;
            ApiPollingTrigger?.Invoke(this, true);
        }
    }
}
