using MarkStrendin.EnvCanadaWeatherParser;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnvironmentCanadaWeatherFunction
{
    class CachedCurrentWeatherResult
    {
        public DateTime timeCached { get; set; }
        public CurrentWeather CurrentWeather { get; set; }
        public bool IsSuccess { get; set; }
    }
}
