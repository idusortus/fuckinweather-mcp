namespace FuknWeather.Api.Models;

/// <summary>
/// Content rating for weather descriptions.
/// </summary>
public enum Rating
{
    /// <summary>
    /// G-rated: General audiences, family-friendly.
    /// </summary>
    G,
    
    /// <summary>
    /// PG-rated: Parental guidance suggested, mild content.
    /// </summary>
    PG,
    
    /// <summary>
    /// PG-13 rated: Parents strongly cautioned, moderate content.
    /// </summary>
    PG13,
    
    /// <summary>
    /// R-rated: Restricted, strong content.
    /// </summary>
    R,
    
    /// <summary>
    /// X-rated: Explicit content, very strong language.
    /// </summary>
    X,
    
    /// <summary>
    /// Bland: Painfully bland, robotic descriptions.
    /// </summary>
    BLAND
}
