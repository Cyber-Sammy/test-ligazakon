namespace NotificationService.Common;

internal static class Constants
{
    internal static class Results
    {
        internal const string SuccessCannotContainMessage =
            "A successful result cannot contain an error message.";
        internal const string FailureMustContainMessage =
            "A failed result must contain an error message.";
        internal const string FailedResultHasNoValue =
            "A failed result does not contain a value.";
        internal const string CannotCreateFailureFromSuccess =
            "A failed result cannot be created from a successful result.";
        internal const string SuccessStatusCannotRepresentFailure =
            "Success status cannot be used to create a failed result.";
    }

    internal static class Persistence
    {
        internal const string DefaultConnectionName = "DefaultConnection";
        internal const string ConnectionStringNotConfigured =
            "Connection string '{0}' is not configured.";
    }

    internal static class Inbox
    {
        internal const string Table = "InboxMessages";
        internal const int EventTypeMaxLength = 200;
        internal const string VersionCheck = "CK_InboxMessages_EventVersion_Positive";
        internal const string VersionCheckSql = "\"EventVersion\" > 0";
        internal const string ProcessingTimeCheck = "CK_InboxMessages_ProcessingTime_Valid";
        internal const string ProcessingTimeCheckSql =
            "\"ProcessedAtUtc\" >= \"ReceivedAtUtc\"";
        internal const string EmptyEventId = "Inbox event ID cannot be empty.";
        internal const string TimestampMustUseUtc = "Timestamp '{0}' must use the UTC offset.";
        internal const string ProcessingBeforeReceipt =
            "The processing timestamp cannot be earlier than the receipt timestamp.";
    }

    internal static class IntegrationEvents
    {
        internal const string UserRegisteredType = "user.registered";
        internal const int UserRegisteredVersion = 1;
    }

    internal static class Kafka
    {
        internal const string ClientId = "notification-service";
        internal const string JsonContentType = "application/json";
        internal const string EventIdFormat = "D";
        internal const string TimestampFormat = "O";
        internal const string BootstrapServersNotConfigured = "Kafka bootstrap servers are not configured.";
        internal const string GroupIdNotConfigured = "Kafka consumer group ID is not configured.";
        internal const string UserEventsTopicNotConfigured = "Kafka user-events topic is not configured.";
        internal const string ProcessingRetryDelayMustBePositive =
            "Kafka message processing retry delay must be greater than zero.";
        internal const string FatalError = "Fatal Kafka consumer error {ErrorCode}: {Reason}.";
        internal const string RequiredHeaderMissing = "Required Kafka header '{0}' is missing.";
        internal const string InvalidHeader = "Kafka header '{0}' is invalid.";
        internal const string UnsupportedContentType = "Kafka content type '{0}' is not supported.";
        internal const string EmptyPayload = "Kafka message payload is empty.";
        internal const string InvalidPayload = "Kafka message payload is invalid for event type '{0}'.";

        internal static class Headers
        {
            internal const string EventId = "event-id";
            internal const string EventType = "event-type";
            internal const string EventVersion = "event-version";
            internal const string OccurredAtUtc = "occurred-at-utc";
            internal const string ContentType = "content-type";
        }
    }

    internal static class Smtp
    {
        internal const string HostNotConfigured = "SMTP host is not configured.";
        internal const string PortOutOfRange = "SMTP port must be between 1 and 65535.";
        internal const string SenderNameNotConfigured = "SMTP sender name is not configured.";
        internal const string SenderAddressNotConfigured = "SMTP sender address is not configured.";
    }

    internal static class Email
    {
        internal const string UserRegisteredSubject = "Registration completed";
        internal const string UserRegisteredBody =
            "Hello, {0}! Your registration was successful.";
    }

    internal static class Logging
    {
        internal const string ConsumerSubscribed =
            "Kafka consumer subscribed to topic {Topic} at {SubscribedAtUtc}.";
        internal const string ConsumerStopped = "Kafka consumer stopped.";
        internal const string InvalidKafkaHeaders =
            "Skipping Kafka message {TopicPartitionOffset} because its headers are invalid: {Reason}";
        internal const string InvalidKafkaPayload =
            "Skipping integration event {EventId} because its payload is invalid: {Reason}";
        internal const string UnsupportedIntegrationEvent =
            "Skipping unsupported integration event {EventId} of type {EventType} version {EventVersion}.";
        internal const string IntegrationEventProcessingFailed =
            "Failed to process integration event {EventId} at {TopicPartitionOffset}; retrying in {RetryDelaySeconds} seconds.";
        internal const string IntegrationEventAlreadyProcessed =
            "Integration event {EventId} has already been processed.";
    }
}
