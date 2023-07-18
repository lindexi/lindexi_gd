using KawlarjichiJemcohoyem.DataContracts;

using System.Collections.Immutable;

namespace KawlarjichiJemcohoyem.Services.Caching;
public interface IWeatherCache
{
    ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token);
}
