using System;
using System.Collections.Generic;
using Microsoft.AppCenter.Analytics;
using Prism.Logging;

namespace ExhibitorModule.Services
{
    public class MCAnalyticsLogger : ILoggerFacade
    {
        public void Log(string message, Category category, Priority priority)
        {
            Analytics.TrackEvent($"{category}", new Dictionary<string, string>
            {
                { "logger", nameof(ILoggerFacade) },
                { "priority", $"{priority}" },
                { "message", message }
            });
        }
    }
}