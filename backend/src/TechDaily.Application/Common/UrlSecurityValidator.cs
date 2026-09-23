using System.Net;
using System.Net.Sockets;

namespace TechDaily.Application.Common;

public static class UrlSecurityValidator
{
    public static (Uri ValidatedUri, IPAddress[] ResolvedIps) ValidateSafeUrl(string url)
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

        IPAddress[] resolvedIps;
        if (IPAddress.TryParse(host, out var ip))
        {
            if (IsPrivateOrRestrictedIp(ip))
            {
                throw new InvalidOperationException("Requests to private or loopback IP addresses are prohibited.");
            }
            resolvedIps = new[] { ip };
        }
        else
        {
            try
            {
                resolvedIps = Dns.GetHostAddresses(host);
                if (resolvedIps == null || resolvedIps.Length == 0)
                {
                    throw new InvalidOperationException("DNS resolution returned no IP addresses.");
                }

                if (resolvedIps.Any(IsPrivateOrRestrictedIp))
                {
                    throw new InvalidOperationException("Requests to internal or private IP addresses are prohibited.");
                }
            }
            catch (SocketException)
            {
                // DNS lookup failure will be handled gracefully during HTTP fetch (or offline unit test mocks)
                resolvedIps = Array.Empty<IPAddress>();
            }
        }

        return (uri, resolvedIps);
    }

    public static bool IsPrivateOrRestrictedIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();

        var bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            // 0.0.0.0/8 (Current network / unspecified)
            if (bytes[0] == 0) return true;
            // 10.0.0.0/8 (Private)
            if (bytes[0] == 10) return true;
            // 100.64.0.0/10 (Shared / CGNAT)
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return true;
            // 127.0.0.0/8 (Loopback)
            if (bytes[0] == 127) return true;
            // 169.254.0.0/16 (Link-local)
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            // 172.16.0.0/12 (Private)
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            // 192.168.0.0/16 (Private)
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            // 224.0.0.0/4 (Multicast)
            if (bytes[0] >= 224 && bytes[0] <= 239) return true;
            // 255.255.255.255 (Broadcast)
            if (bytes[0] == 255 && bytes[1] == 255 && bytes[2] == 255 && bytes[3] == 255) return true;
        }
        else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // :: (Unspecified)
            if (ip.Equals(IPAddress.IPv6None) || bytes.All(b => b == 0)) return true;
            // ::1 (Loopback)
            if (IPAddress.IsLoopback(ip)) return true;
            // fe80::/10 (Link-local), fec0::/10 (Site-local)
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true;
            // fc00::/7 (Unique local)
            if (bytes[0] == 0xfc || bytes[0] == 0xfd) return true;
            // ff00::/8 (Multicast)
            if (bytes[0] == 0xff) return true;
        }

        return false;
    }
}
