using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace FileMonitorService.Middleware;

/// <summary>
/// Middleware для ограничения доступа по IP адресам
/// </summary>
public class IpFilterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpFilterMiddleware> _logger;
    private readonly List<IPNetwork> _allowedNetworks;
    private readonly bool _enabled;

    public IpFilterMiddleware(
        RequestDelegate next,
        ILogger<IpFilterMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _allowedNetworks = new List<IPNetwork>();

        var networks = configuration.GetSection("Security:AllowedNetworks").Get<string[]>();
        
        if (networks != null && networks.Length > 0)
        {
            foreach (var network in networks)
            {
                try
                {
                    var parsed = ParseNetwork(network);
                    if (parsed != null)
                    {
                        _allowedNetworks.Add(parsed);
                        _logger.LogInformation("Allowed network: {Network}", network);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse network: {Network}", network);
                }
            }
            _enabled = _allowedNetworks.Count > 0;
        }

        if (!_enabled)
        {
            _logger.LogWarning("IP filtering DISABLED - set Security:AllowedNetworks in appsettings.json");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enabled)
        {
            await _next(context);
            return;
        }

        var remoteIp = context.Connection.RemoteIpAddress;
        
        if (remoteIp == null)
        {
            await _next(context);
            return;
        }

        // Преобразуем IPv6 localhost в IPv4
        if (IPAddress.IsLoopback(remoteIp))
        {
            await _next(context);
            return;
        }

        // Проверяем, входит ли IP в разрешённые сети
        var allowed = _allowedNetworks.Any(n => n.Contains(remoteIp));

        if (!allowed)
        {
            _logger.LogWarning("Blocked request from {IP}", remoteIp);
            
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new 
            { 
                error = "Access denied",
                message = "Your IP is not in the allowed list"
            });
            return;
        }

        await _next(context);
    }

    private IPNetwork? ParseNetwork(string network)
    {
        if (string.IsNullOrWhiteSpace(network))
            return null;

        // Формат: 10.33.0.0/16 или 192.168.1.0/24
        var parts = network.Split('/');
        if (parts.Length != 2)
            return null;

        if (!IPAddress.TryParse(parts[0], out var address))
            return null;

        if (!int.TryParse(parts[1], out var prefixLength))
            return null;

        return new IPNetwork(address, prefixLength);
    }

    private class IPNetwork
    {
        private readonly IPAddress _network;
        private readonly int _prefixLength;
        private readonly byte[] _networkBytes;
        private readonly byte[] _mask;

        public IPNetwork(IPAddress network, int prefixLength)
        {
            _network = network;
            _prefixLength = prefixLength;
            _networkBytes = network.GetAddressBytes();
            _mask = CreateMask(prefixLength, _networkBytes.Length);
        }

        public bool Contains(IPAddress address)
        {
            var addressBytes = address.GetAddressBytes();
            
            // Разные семейства адресов
            if (addressBytes.Length != _networkBytes.Length)
            {
                // Попробуем извлечь IPv4 из IPv6-mapped
                if (address.IsIPv4MappedToIPv6)
                {
                    addressBytes = address.MapToIPv4().GetAddressBytes();
                    if (addressBytes.Length != _networkBytes.Length)
                        return false;
                }
                else
                {
                    return false;
                }
            }

            for (int i = 0; i < _networkBytes.Length; i++)
            {
                if ((_networkBytes[i] & _mask[i]) != (addressBytes[i] & _mask[i]))
                    return false;
            }

            return true;
        }

        private byte[] CreateMask(int prefixLength, int length)
        {
            var mask = new byte[length];
            var remainingBits = prefixLength;

            for (int i = 0; i < length; i++)
            {
                if (remainingBits >= 8)
                {
                    mask[i] = 0xFF;
                    remainingBits -= 8;
                }
                else if (remainingBits > 0)
                {
                    mask[i] = (byte)(0xFF << (8 - remainingBits));
                    remainingBits = 0;
                }
                else
                {
                    mask[i] = 0;
                }
            }

            return mask;
        }
    }
}

public static class IpFilterMiddlewareExtensions
{
    public static IApplicationBuilder UseIpFiltering(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IpFilterMiddleware>();
    }
}
