namespace FuknWeather.Api.Services;

/// <summary>
/// Service that generates colorful, NSFW weather descriptions based on temperature.
/// </summary>
public class WeatherDescriptionService
{
    /// <summary>
    /// Gets a colorful, NSFW description based on the given temperature.
    /// </summary>
    /// <param name="temperature">Temperature in Fahrenheit.</param>
    /// <returns>A colorful weather description.</returns>
    public string GetColorfulDescription(decimal temperature)
    {
        return temperature switch
        {
            < 0 => "It's colder than a witch's tit in a brass bra! Fucking freezing out there!",
            < 20 => "It's ball-shriveling cold! Bundle the fuck up!",
            < 32 => "Freezing your ass off weather. Don't be a dumbass, wear a coat!",
            < 40 => "Cold as fuck! Winter can eat a dick.",
            < 50 => "Pretty damn chilly. Jacket up, buttercup!",
            < 60 => "Kinda cool, not too shabby. Tolerable as hell.",
            < 70 => "Actually pretty fucking nice out! Get your ass outside!",
            < 80 => "Beautiful as fuck! Perfect weather for not being a hermit!",
            < 85 => "Warm and wonderful! Mother Nature's not being a bitch today!",
            < 90 => "Getting hot as balls! Shorts and tank top weather!",
            < 95 => "Hot as fuck! Stay hydrated, you dehydrated bastard!",
            < 100 => "Hotter than Satan's asshole! AC is your best friend!",
            < 110 => "Ungodly fucking hot! Are you living in hell?",
            _ => "What in the actual fuck?! This temperature is apocalyptic! Stay inside or die!"
        };
    }
}
