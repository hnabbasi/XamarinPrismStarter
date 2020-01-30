using System;
using Prism.Logging;
using static System.Diagnostics.Debug;

namespace ExhibitorModule.Services
{
    public class DebugLogger : ILoggerFacade
    {
        const string TAG = "[DEBUG]";

        public void Log(string message, Category category, Priority priority) =>
            WriteLine($"{TAG} - {category} - {priority}: {message}");
    }
}