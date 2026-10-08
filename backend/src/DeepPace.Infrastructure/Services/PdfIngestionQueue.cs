using System.Threading.Channels;
using DeepPace.Application.Interfaces;

namespace DeepPace.Infrastructure.Services;

public class PdfIngestionQueue : IPdfIngestionQueue
{
    private readonly Channel<PdfIngestJob> _channel = Channel.CreateUnbounded<PdfIngestJob>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(PdfIngestJob job, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public IAsyncEnumerable<PdfIngestJob> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
