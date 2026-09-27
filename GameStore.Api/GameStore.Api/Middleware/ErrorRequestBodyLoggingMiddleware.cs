using System.Text;

namespace GameStore.Api.Middleware;

public sealed class ErrorRequestBodyLoggingMiddleware(RequestDelegate next)
{
    public const string RequestBodyItemKey = "ErrorRequestBody";
    public const string ResponseBodyItemKey = "ErrorResponseBody";
    private const int MaximumBodyBytes = 4096;

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var contentType = request.ContentType?.Split(';', 2)[0].Trim();

        if (request.ContentLength is > 0 and <= MaximumBodyBytes && IsJson(contentType))
        {
            request.EnableBuffering(
                bufferThreshold: MaximumBodyBytes,
                bufferLimit: MaximumBodyBytes);

            await using var buffer = new MemoryStream((int)request.ContentLength.Value);
            await request.Body.CopyToAsync(buffer, context.RequestAborted);
            request.Body.Position = 0;
            context.Items[RequestBodyItemKey] = Encoding.UTF8.GetString(buffer.ToArray());
        }

        var originalResponseBody = context.Response.Body;
        await using var responseCapture = new BoundedCaptureStream(originalResponseBody, MaximumBodyBytes);
        context.Response.Body = responseCapture;

        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalResponseBody;
        }

        if (context.Response.StatusCode >= 400 &&
            responseCapture.CapturedLength > 0 &&
            IsJson(context.Response.ContentType?.Split(';', 2)[0].Trim()))
        {
            var responseBody = Encoding.UTF8.GetString(responseCapture.GetCapturedBytes());
            if (responseCapture.IsTruncated)
            {
                responseBody += " [truncated]";
            }

            context.Items[ResponseBodyItemKey] = responseBody;
        }
    }

    private static bool IsJson(string? contentType) =>
        contentType is not null &&
        (contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
         (contentType.StartsWith("application/", StringComparison.OrdinalIgnoreCase) &&
          contentType.EndsWith("+json", StringComparison.OrdinalIgnoreCase)));

    private sealed class BoundedCaptureStream(Stream destination, int maximumBytes) : Stream
    {
        private readonly MemoryStream _captured = new();

        public int CapturedLength => (int)_captured.Length;
        public bool IsTruncated { get; private set; }

        public byte[] GetCapturedBytes() => _captured.ToArray();

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => destination.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            destination.FlushAsync(cancellationToken);

        public override void Write(byte[] buffer, int offset, int count)
        {
            Capture(buffer.AsSpan(offset, count));
            destination.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Capture(buffer);
            destination.Write(buffer);
        }

        public override async Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            Capture(buffer.AsSpan(offset, count));
            await destination.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
        }

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            Capture(buffer.Span);
            await destination.WriteAsync(buffer, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _captured.Dispose();
            }

            base.Dispose(disposing);
        }

        private void Capture(ReadOnlySpan<byte> bytes)
        {
            var remaining = maximumBytes - CapturedLength;
            var bytesToCapture = Math.Min(remaining, bytes.Length);
            if (bytesToCapture > 0)
            {
                _captured.Write(bytes[..bytesToCapture]);
            }

            if (bytesToCapture < bytes.Length)
            {
                IsTruncated = true;
            }
        }
    }
}