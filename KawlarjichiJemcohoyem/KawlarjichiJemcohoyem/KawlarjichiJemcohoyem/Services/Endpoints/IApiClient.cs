using KawlarjichiJemcohoyem.DataContracts;

using System.Collections.Immutable;

namespace KawlarjichiJemcohoyem.Services.Endpoints;
[Headers("Content-Type: application/json")]
public interface IApiClient
{
    [Get("/api/weatherforecast")]
    Task<ApiResponse<IImmutableList<WeatherForecast>>> GetWeather(CancellationToken cancellationToken = default);
}
