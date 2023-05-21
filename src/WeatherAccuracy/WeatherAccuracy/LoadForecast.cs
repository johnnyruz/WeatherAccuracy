using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ITableEntity = Azure.Data.Tables.ITableEntity;

namespace WeatherAccuracy
{
    public class LoadForecast
    {

        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("LoadForecast")]
        public static async Task Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, ILogger log)
        {

            try
            {
                // Make a web request to retrieve data
                var forecastData = await GetDataFromWeb();

                // Store the data in Azure Table Storage
                await StoreDataInTableStorage(forecastData);

                log.LogInformation("Data stored successfully.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while processing the function.");
            }

        }

        private static async Task<WeatherData> GetDataFromWeb()
        {
            // Make an HTTP GET request to retrieve data from a web API
            var endpointUrl = $"{Environment.GetEnvironmentVariable("WEATHER_API_ENDPOINT").TrimEnd('/')}" +
                                $"/forecast.json?key={Environment.GetEnvironmentVariable("WEATHER_API_KEY")}" +
                                $"&q={Environment.GetEnvironmentVariable("ZIP_CODE")}&days=10&aqi=no&alerts=no";
            var response = await httpClient.GetAsync(endpointUrl);

            // Ensure a successful response
            response.EnsureSuccessStatusCode();

            // Read the response content as string
            var data = await response.Content.ReadAsStringAsync();
            var weatherObject = JsonConvert.DeserializeObject<WeatherData>(data);

            return weatherObject;
        }
        
        private static async Task StoreDataInTableStorage(WeatherData data)
        {
            // Create a new instance of the TableClient
            var tableClient = new TableServiceClient(Environment.GetEnvironmentVariable("TABLE_STORAGE_CONN_STRING"));
            var table = tableClient.GetTableClient(Environment.GetEnvironmentVariable("TABLE_STORAGE_TABLE_NAME"));

            DateTime utcTime = DateTime.UtcNow;
            TimeZoneInfo targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Environment.GetEnvironmentVariable("TARGET_TIME_ZONE"));
            DateTime targetTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, targetTimeZone);

            // Create a new entity with the data to be stored
            foreach (var day in data.Forecast.ForecastDay) {
                var entity = new WeatherEntity
                {
                    PartitionKey = day.Date.ToString(),
                    RowKey = targetTime.ToString("yyyy-MM-dd hh:mm:ss tt"),
                    MinTempC = day.Day.MinTempC,
                    MinTempF = day.Day.MinTempF,
                    MaxTempC = day.Day.MaxTempC,
                    MaxTempF = day.Day.MaxTempF,
                    TotalRainInches = day.Day.TotalPrecipIn,
                    ChanceOfRain = day.Day.DailyChanceOfRain,
                    WillItRain = day.Day.DailyWillItRain == 1 ? true : false,
                    Type = "FORECAST"
                };

                // Insert the entity into the table
                await table.AddEntityAsync(entity);
            }
        }
    }

    public class WeatherEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = default!;
        public string RowKey { get; set; } = default!;
        public DateTimeOffset? Timestamp { get; set; } = default!;
        ETag ITableEntity.ETag { get; set; } = default!;

        public double MinTempC { get; set; }
        public double MinTempF { get; set; }
        public double MaxTempC { get; set; }
        public double MaxTempF { get; set; }
        public double TotalRainInches { get; set; }
        public double ChanceOfRain { get; set; }
        public bool WillItRain { get; set; }
        public string Type { get; set; }
    }

    public class Location
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("lat")]
        public double Lat { get; set; }

        [JsonProperty("lon")]
        public double Lon { get; set; }

        [JsonProperty("tz_id")]
        public string TzId { get; set; }

        [JsonProperty("localtime_epoch")]
        public long LocaltimeEpoch { get; set; }

        [JsonProperty("localtime")]
        public string Localtime { get; set; }
    }

    public class Condition
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }
    }

    public class Current
    {
        [JsonProperty("last_updated_epoch")]
        public long LastUpdatedEpoch { get; set; }

        [JsonProperty("last_updated")]
        public string LastUpdated { get; set; }

        [JsonProperty("temp_c")]
        public double TempC { get; set; }

        [JsonProperty("temp_f")]
        public double TempF { get; set; }

        [JsonProperty("is_day")]
        public int IsDay { get; set; }

        [JsonProperty("condition")]
        public Condition Condition { get; set; }

        [JsonProperty("wind_mph")]
        public double WindMph { get; set; }

        [JsonProperty("wind_kph")]
        public double WindKph { get; set; }

        [JsonProperty("wind_degree")]
        public int WindDegree { get; set; }

        [JsonProperty("wind_dir")]
        public string WindDir { get; set; }

        [JsonProperty("pressure_mb")]
        public double PressureMb { get; set; }

        [JsonProperty("pressure_in")]
        public double PressureIn { get; set; }

        [JsonProperty("precip_mm")]
        public double PrecipMm { get; set; }

        [JsonProperty("precip_in")]
        public double PrecipIn { get; set; }

        [JsonProperty("humidity")]
        public int Humidity { get; set; }

        [JsonProperty("cloud")]
        public int Cloud { get; set; }

        [JsonProperty("feelslike_c")]
        public double FeelslikeC { get; set; }

        [JsonProperty("feelslike_f")]
        public double FeelslikeF { get; set; }

        [JsonProperty("vis_km")]
        public double VisKm { get; set; }

        [JsonProperty("vis_miles")]
        public double VisMiles { get; set; }

        [JsonProperty("uv")]
        public double Uv { get; set; }

        [JsonProperty("gust_mph")]
        public double GustMph { get; set; }

        [JsonProperty("gust_kph")]
        public double GustKph { get; set; }
    }

    public class DailyForecast
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("date_epoch")]
        public double DateEpoch { get; set; }

        [JsonProperty("day")]
        public Day Day { get; set; }

    }
    public class Day
    {
        [JsonProperty("maxtemp_c")]
        public double MaxTempC { get; set; }

        [JsonProperty("maxtemp_f")]
        public double MaxTempF { get; set; }

        [JsonProperty("mintemp_c")]
        public double MinTempC { get; set; }

        [JsonProperty("mintemp_f")]
        public double MinTempF { get; set; }

        [JsonProperty("avgtemp_c")]
        public double AvgTempC { get; set; }

        [JsonProperty("avgtemp_f")]
        public double AvgTempF { get; set; }

        [JsonProperty("maxwind_mph")]
        public double MaxWindMph { get; set; }

        [JsonProperty("maxwind_kph")]
        public double MaxWindKph { get; set; }

        [JsonProperty("totalprecip_mm")]
        public double TotalPrecipMm { get; set; }

        [JsonProperty("totalprecip_in")]
        public double TotalPrecipIn { get; set; }

        [JsonProperty("totalsnow_cm")]
        public double TotalSnowCm { get; set; }

        [JsonProperty("avgvis_km")]
        public double AvgVisKm { get; set; }

        [JsonProperty("avgvis_miles")]
        public double AvgVisMiles { get; set; }

        [JsonProperty("avghumidity")]
        public double AvgHumidity { get; set; }

        [JsonProperty("daily_will_it_rain")]
        public int DailyWillItRain { get; set; }

        [JsonProperty("daily_chance_of_rain")]
        public int DailyChanceOfRain { get; set; }

        [JsonProperty("daily_will_it_snow")]
        public int DailyWillItSnow { get; set; }

        [JsonProperty("daily_chance_of_snow")]
        public int DailyChanceOfSnow { get; set; }

        [JsonProperty("condition")]
        public Condition Condition { get; set; }

        [JsonProperty("uv")]
        public double Uv { get; set; }
    }

    public class Forecast
    {
        [JsonProperty("forecastday")]
        public List<DailyForecast> ForecastDay { get; set; }
    }

    public class WeatherData
    {
        [JsonProperty("location")]
        public Location Location { get; set; }

        [JsonProperty("current")]
        public Current Current { get; set; }

        [JsonProperty("forecast")]
        public Forecast Forecast { get; set; }
    }


}
