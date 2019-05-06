using MarkStrendin.EnvCanadaWeatherParser;
using System;

namespace EnvironmentCanadaWeatherFunction
{
    class CachedCurrentWeatherResult
    {
        public DateTime TimeCached { get; set; }
        public CurrentWeather CurrentWeather { get; set; }
        public bool IsSuccess { get; set; }
    }
}
