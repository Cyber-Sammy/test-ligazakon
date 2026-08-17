using System.Globalization;
using System.Text;
using Confluent.Kafka;
using NotificationService.Common.Results;
using NotificationService.Kafka;

namespace NotificationService.Tests.Kafka;

public sealed class KafkaMessageHeaderParserTests
{
    [Fact]
    public void Parse_ValidHeaders_ReturnsMetadata()
    {
        var eventId = Guid.NewGuid();
        var occurredAtUtc = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);
        var headers = CreateHeaders(eventId, occurredAtUtc);

        var result = KafkaMessageHeaderParser.Parse(headers);

        Assert.True(result.IsSuccessfull);
        Assert.Equal(eventId, result.Value.EventId);
        Assert.Equal("user.registered", result.Value.EventType);
        Assert.Equal(1, result.Value.EventVersion);
        Assert.Equal(occurredAtUtc, result.Value.OccurredAtUtc);
    }

    [Fact]
    public void Parse_MissingHeader_ReturnsValidationError()
    {
        var headers = CreateHeaders(Guid.NewGuid(), DateTimeOffset.UtcNow);
        headers.Remove("event-id");

        var result = KafkaMessageHeaderParser.Parse(headers);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Contains("event-id", result.Message);
    }

    [Fact]
    public void Parse_InvalidVersion_ReturnsValidationError()
    {
        var headers = CreateHeaders(Guid.NewGuid(), DateTimeOffset.UtcNow);
        headers.Remove("event-version");
        headers.Add("event-version", Encode("0"));

        var result = KafkaMessageHeaderParser.Parse(headers);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Contains("event-version", result.Message);
    }

    [Fact]
    public void Parse_NonUtcTimestamp_ReturnsValidationError()
    {
        var headers = CreateHeaders(Guid.NewGuid(), DateTimeOffset.UtcNow);
        headers.Remove("occurred-at-utc");
        headers.Add("occurred-at-utc", Encode("2026-08-17T15:00:00.0000000+03:00"));

        var result = KafkaMessageHeaderParser.Parse(headers);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Contains("occurred-at-utc", result.Message);
    }

    [Fact]
    public void Parse_UnsupportedContentType_ReturnsValidationError()
    {
        var headers = CreateHeaders(Guid.NewGuid(), DateTimeOffset.UtcNow);
        headers.Remove("content-type");
        headers.Add("content-type", Encode("text/plain"));

        var result = KafkaMessageHeaderParser.Parse(headers);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Contains("text/plain", result.Message);
    }

    [Fact]
    public void Parse_InvalidUtf8_ReturnsValidationError()
    {
        var headers = CreateHeaders(Guid.NewGuid(), DateTimeOffset.UtcNow);
        headers.Remove("event-type");
        headers.Add("event-type", [0xC3, 0x28]);

        var result = KafkaMessageHeaderParser.Parse(headers);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Contains("event-type", result.Message);
    }

    private static Headers CreateHeaders(Guid eventId, DateTimeOffset occurredAtUtc) =>
    [
        new Header("event-id", Encode(eventId.ToString("D"))),
        new Header("event-type", Encode("user.registered")),
        new Header("event-version", Encode("1")),
        new Header("occurred-at-utc", Encode(occurredAtUtc.ToString("O", CultureInfo.InvariantCulture))),
        new Header("content-type", Encode("application/json"))
    ];

    private static byte[] Encode(string value) => Encoding.UTF8.GetBytes(value);
}
