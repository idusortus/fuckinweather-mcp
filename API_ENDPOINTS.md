# Weather API Endpoints

## Overview

This API provides weather information with colorful descriptions across multiple content ratings. Each temperature range has 10 different descriptions per rating for variety.

## Content Ratings

- **G** - General audiences, family-friendly language
- **PG** - Parental guidance suggested, mild humor
- **PG-13** - Moderate language and humor
- **R** - Strong language and explicit humor
- **X** - Very explicit content
- **BLAND** - Painfully bland, robotic descriptions (like a 60s computer)

## Endpoints

### 1. Get Weather by Zip Code (Default Rating)

```http
GET /api/weather/{zipCode}
```

Returns current weather for a US zip code with X-rated description (for backward compatibility).

**Example:**
```bash
curl http://localhost:5000/api/weather/10001
```

**Response:**
```json
{
  "zipCode": "10001",
  "temperatureFahrenheit": 72.5,
  "description": "Fucking perfection! Get outside and enjoy this shit!",
  "location": "New York",
  "rating": "X"
}
```

### 2. Get Weather by Zip Code with Rating

```http
GET /api/weather/{zipCode}/{rating}
```

Returns current weather for a US zip code with specified content rating.

**Parameters:**
- `zipCode` - 5-digit US zip code
- `rating` - Content rating: G, PG, PG13, R, X, or BLAND

**Example:**
```bash
curl http://localhost:5000/api/weather/10001/G
```

**Response:**
```json
{
  "zipCode": "10001",
  "temperatureFahrenheit": 72.5,
  "description": "Beautiful day ahead! Perfect weather!",
  "location": "New York",
  "rating": "G"
}
```

### 3. Get Description by Temperature and Rating

```http
GET /api/weather/temperature/{temperature}/{rating}
```

Returns a weather description for a specific temperature and rating.

**Parameters:**
- `temperature` - Temperature in Fahrenheit (-50 to 140)
- `rating` - Content rating: G, PG, PG13, R, X, or BLAND

**Example:**
```bash
curl http://localhost:5000/api/weather/temperature/32/PG
```

**Response:**
```json
{
  "zipCode": "N/A",
  "temperatureFahrenheit": 32,
  "description": "It's nippy out there. Don't be a hero, wear a coat!",
  "location": "N/A",
  "rating": "PG"
}
```

### 4. Get Random Weather with All Ratings

```http
GET /api/weather/random
```

Generates a random temperature and returns descriptions for ALL ratings.

**Example:**
```bash
curl http://localhost:5000/api/weather/random
```

**Response:**
```json
{
  "temperatureFahrenheit": 72,
  "descriptionsByRating": {
    "G": "Beautiful day ahead! Perfect weather!",
    "PG": "Perfect as hell! What are you waiting for?",
    "PG-13": "Damn near perfect weather! Get outside!",
    "R": "Fucking perfect weather! Get outside!",
    "X": "Damn near orgasmic weather! Get moving!",
    "BLAND": "Temperature reading: Optimal conditions detected. Ideal range."
  }
}
```

## Temperature Ranges

The API covers temperatures from **-50°F to 140°F** in **5-degree increments** (39 total ranges).

Each range has:
- 10 different descriptions per rating
- Randomized selection for variety

## Example Use Cases

### Family-Friendly Weather App
```bash
# Always use G rating
curl http://localhost:5000/api/weather/90210/G
```

### Weather Bot with Personality
```bash
# Use PG-13 or R for humor without being too explicit
curl http://localhost:5000/api/weather/60601/PG13
```

### IoT Sensor Display
```bash
# Get temperature from sensor, display with BLAND rating
curl http://localhost:5000/api/weather/temperature/68/BLAND
```

### Demo/Testing
```bash
# Show all rating styles at once
curl http://localhost:5000/api/weather/random
```

## Error Responses

### Invalid Zip Code
```json
{
  "error": "Invalid zip code format. Must be 5 digits."
}
```

### Invalid Rating
```json
{
  "error": "Invalid rating. Valid options: G, PG, PG13, R, X, BLAND"
}
```

### Temperature Out of Range
```json
{
  "error": "Temperature must be between -50°F and 140°F"
}
```

## Notes

- All descriptions are randomly selected from a pool of 10 per temperature range
- The API maintains the same personality/tone within each rating across all temperatures
- Temperature ranges are automatically clamped to -50°F to 140°F for safety
- G and BLAND ratings contain no profanity
- PG contains mild language
- PG-13 contains moderate language
- R contains strong language
- X contains very explicit content
