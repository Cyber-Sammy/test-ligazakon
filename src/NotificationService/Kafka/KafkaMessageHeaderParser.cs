using System.Globalization;
using System.Text;
using Confluent.Kafka;
using NotificationService.Common;
using NotificationService.Common.Results;

namespace NotificationService.Kafka;

public static class KafkaMessageHeaderParser
{
    public static Result<KafkaMessageMetadata> Parse(Headers headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var eventIdHeader = GetRequiredValue(headers, Constants.Kafka.Headers.EventId);
        if (eventIdHeader.IsFailure)
        {
            return Result<KafkaMessageMetadata>.Failure(eventIdHeader);
        }

        var eventTypeHeader = GetRequiredValue(headers, Constants.Kafka.Headers.EventType);
        if (eventTypeHeader.IsFailure)
        {
            return Result<KafkaMessageMetadata>.Failure(eventTypeHeader);
        }

        var eventVersionHeader = GetRequiredValue(headers, Constants.Kafka.Headers.EventVersion);
        if (eventVersionHeader.IsFailure)
        {
            return Result<KafkaMessageMetadata>.Failure(eventVersionHeader);
        }

        var occurredAtHeader = GetRequiredValue(headers, Constants.Kafka.Headers.OccurredAtUtc);
        if (occurredAtHeader.IsFailure)
        {
            return Result<KafkaMessageMetadata>.Failure(occurredAtHeader);
        }

        var contentTypeHeader = GetRequiredValue(headers, Constants.Kafka.Headers.ContentType);
        if (contentTypeHeader.IsFailure)
        {
            return Result<KafkaMessageMetadata>.Failure(contentTypeHeader);
        }

        if (!Guid.TryParseExact(
                eventIdHeader.Value,
                Constants.Kafka.EventIdFormat,
                out var eventId)
            || eventId == Guid.Empty)
        {
            return InvalidMetadata(Constants.Kafka.Headers.EventId);
        }

        if (!int.TryParse(
                eventVersionHeader.Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var eventVersion)
            || eventVersion <= 0)
        {
            return InvalidMetadata(Constants.Kafka.Headers.EventVersion);
        }

        if (!DateTimeOffset.TryParseExact(
                occurredAtHeader.Value,
                Constants.Kafka.TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var occurredAtUtc)
            || occurredAtUtc.Offset != TimeSpan.Zero)
        {
            return InvalidMetadata(Constants.Kafka.Headers.OccurredAtUtc);
        }

        if (!string.Equals(
                contentTypeHeader.Value,
                Constants.Kafka.JsonContentType,
                StringComparison.OrdinalIgnoreCase))
        {
            return Result<KafkaMessageMetadata>.Failure(
                ResultStatus.ValidationError,
                string.Format(
                    Constants.Kafka.UnsupportedContentType,
                    contentTypeHeader.Value));
        }

        return Result<KafkaMessageMetadata>.Success(new KafkaMessageMetadata(
            eventId,
            eventTypeHeader.Value,
            eventVersion,
            occurredAtUtc));
    }

    private static Result<string> GetRequiredValue(Headers headers, string key)
    {
        var header = headers.LastOrDefault(candidate => candidate.Key == key);
        var valueBytes = header?.GetValueBytes();

        if (valueBytes is null)
        {
            return Result<string>.Failure(
                ResultStatus.ValidationError,
                string.Format(Constants.Kafka.RequiredHeaderMissing, key));
        }

        try
        {
            var value = Utf8.GetString(valueBytes);

            return string.IsNullOrWhiteSpace(value)
                ? Result<string>.Failure(
                    ResultStatus.ValidationError,
                    string.Format(Constants.Kafka.InvalidHeader, key))
                : Result<string>.Success(value);
        }
        catch (DecoderFallbackException)
        {
            return Result<string>.Failure(
                ResultStatus.ValidationError,
                string.Format(Constants.Kafka.InvalidHeader, key));
        }
    }

    private static Result<KafkaMessageMetadata> InvalidMetadata(string headerName) =>
        Result<KafkaMessageMetadata>.Failure(
            ResultStatus.ValidationError,
            string.Format(Constants.Kafka.InvalidHeader, headerName));

    private static readonly Encoding Utf8 = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
}
