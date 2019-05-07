using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MarkStrendin.EnvCanadaWeatherParser;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace EnvironmentCanadaWeatherFunction
{
    public static class GetWeather
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly SortedDictionary<string, CachedCurrentWeatherResult> _cachedResults = new SortedDictionary<string, CachedCurrentWeatherResult>();
        private static readonly CurrentWeather _nullWeather = new CurrentWeather();
        private static readonly List<string> _validLocationPrefixes = new List<string>() { "AB", "BC", "MB", "NB", "NL", "NT", "NS", "NU", "ON", "PE", "QC", "SK", "YT" };
        private static readonly TimeSpan _cacheLifetimeMinutes = new TimeSpan(0, 30, 0);

        [FunctionName("GetWeather")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetWeather/{locationCode}")] HttpRequest req,
            string locationCode,
            ILogger log)
        {
            if (string.IsNullOrEmpty(locationCode))
            {
                log.LogInformation("Recieved request with empty location code.");
                return new JsonResult(_nullWeather);
            }

            return new JsonResult(getWeatherFor(locationCode, log).Result);
        }

        private static async Task<CurrentWeather> getWeatherFor(string locationCode, ILogger log)
        {
            log.LogInformation("Recieved request for location code \"" + locationCode + "\"");
            if (validateLocationCode(locationCode))
            {
                // Check if the value exists in the cache
                if (_cachedResults.ContainsKey(locationCode))
                {
                    CachedCurrentWeatherResult cachedValue = _cachedResults[locationCode];
                    if (DateTime.Now.Subtract(cachedValue.TimeCached) < _cacheLifetimeMinutes)
                    {
                        // Send the cache
                        log.LogInformation("Found cached data for location code \"" + locationCode + "\" - sending");
                        return cachedValue.CurrentWeather;                        
                    }
                }

                // Refresh the cache
                if (_cachedResults.ContainsKey(locationCode))
                {
                    _cachedResults.Remove(locationCode);
                }

                log.LogInformation("No cached data for location code \"" + locationCode + "\" yet - gathering");
                try
                {
                    string xmlBody = await _httpClient.GetStringAsync("https://weather.gc.ca/rss/city/" + locationCode + "_e.xml");
                    EnvCanadaCurrentWeatherParser parser = new EnvCanadaCurrentWeatherParser();
                    CurrentWeather parsedCurrentWeather = parser.ParseXML(xmlBody);

                    // If we've made it this far, add the weather to the cache before returning it
                    CachedCurrentWeatherResult newResult = new CachedCurrentWeatherResult()
                    {
                        TimeCached = DateTime.Now,
                        CurrentWeather = parsedCurrentWeather,
                        IsSuccess = true
                    };
                    log.LogInformation("Recieved valid data for location code \"" + locationCode + "\" yet - caching");
                    _cachedResults.Add(locationCode, newResult);
                    return parsedCurrentWeather;
                } catch
                {
                    // If the value didn't work, store a null in the dictionary instead, to indicate that it didn't work
                    // So we don't keep hammering Environment Canada with invalid codes (or those invalid requests are at least
                    // limited every 10 minutes)
                    CachedCurrentWeatherResult newResult = new CachedCurrentWeatherResult()
                    {
                        TimeCached = DateTime.Now,
                        CurrentWeather = _nullWeather,
                        IsSuccess = false
                    };
                    log.LogInformation("Recieved invalid data for location code \"" + locationCode + "\" yet - caching empty object, will retry after " + DateTime.Now.Add(_cacheLifetimeMinutes));
                    _cachedResults.Add(locationCode, newResult);
                    return _nullWeather;
                }
            } else
            {
                log.LogInformation("Invalid location code: \"" + locationCode + "\"");
                return _nullWeather;
            }
        }

        private static bool validateLocationCode(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                if (input.Length == 5)
                {
                    List<string> splitLocation = input.Split('-').ToList<string>();
                    if (splitLocation.Count == 2)
                    {
                        if ((splitLocation[0].Length == 2) && (splitLocation[1].Length == 2))
                        {
                            if (_validLocationPrefixes.Contains(splitLocation[0].ToUpper()))
                            {
                                return true;
                            }
                        }                        
                    }
                }
            }

            return false;
        }



    }
}
