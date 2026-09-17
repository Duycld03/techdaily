using System.Net;
using System.Net.Sockets;

namespace TechDaily.Application.Common;

public static class UrlSecurityValidator
{
    public static void ValidateSafeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be empty.", nameof(url));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid absolute URL format.", nameof(url));
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Only HTTP and HTTPS protocols are allowed.", nameof(url));
        }

        var host = uri.DnsSafeHost;
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Invalid host in URL.", nameof(url));
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("backend", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("frontend", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("db", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("pgadmin", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Requests to internal hostnames are prohibited.");
        }

        if (IPAddress.TryParse(host, out var ip))
        {
            if (IsPrivateOrRestrictedIp(ip))
            {
                throw new InvalidOperationException("Requests to private or loopback IP addresses are prohibited.");
            }
        }
        else
        {
            try
            {
                var ips = Dns.GetHostAddresses(host);
                if (ips.Any(IsPrivateOrRestrictedIp))
                {
                    throw new InvalidOperationException("Requests to internal or private IP addresses are prohibited.");
                }
            }
            catch (SocketException)
            {
                // DNS lookup failure will be handled gracefully during HTTP fetch
            }
        }
    }

    public static bool IsPrivateOrRestrictedIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();

        var bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            if (bytes[0] == 127) return true;
            if (bytes[0] == 0) return true;
        }
        else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true;
            if (bytes[0] == 0xfc || bytes[0] == 0xfd) return true;
        }

        return false;
    }
}
