namespace UserService.Infrastructure.Common;

internal static class InfrastructureConstants
{
    internal const string DefaultConnectionName = "DefaultConnection";
    internal const string UsersTable = "Users";
    internal const string ConnectionStringNotConfigured = "Connection string '{0}' is not configured.";

    internal static class Logging
    {
        internal const string AddingIntegrationEvent =
            "Staging integration event {EventId} of type {EventType}.";
        internal const string IntegrationEventAdded =
            "Integration event {EventId} of type {EventType} staged.";
        internal const string PublishingIntegrationEvent =
            "Publishing integration event {EventId} of type {EventType}.";
        internal const string IntegrationEventPublished =
            "Integration event {EventId} of type {EventType} published.";
        internal const string ProcessingOutboxBatch =
            "Processing {MessageCount} outbox messages.";
        internal const string OutboxProcessingCancelled =
            "Outbox batch processing was cancelled.";
        internal const string OutboxPublishingFailed =
            "Failed to publish outbox message {MessageId}.";
        internal const string OutboxFailureRegistered =
            "Registered a publishing failure for outbox message {MessageId}; next attempt is scheduled at {NextAttemptAtUtc}.";
    }

    internal static class Kafka
    {
        internal const string ClientId = "user-service";
        internal const string JsonContentType = "application/json";
        internal const string EventIdFormat = "D";
        internal const string TimestampFormat = "O";
        internal const string BootstrapServersNotConfigured = "Kafka bootstrap servers are not configured.";
        internal const string UserEventsTopicNotConfigured = "Kafka user-events topic is not configured.";
        internal const string MessageTimeoutMustBePositive = "Kafka message timeout must be greater than zero.";
        internal const string ProducerFatalError = "Fatal Kafka producer error {ErrorCode}: {Reason}.";

        internal static class Headers
        {
            internal const string EventId = "event-id";
            internal const string EventType = "event-type";
            internal const string EventVersion = "event-version";
            internal const string OccurredAtUtc = "occurred-at-utc";
            internal const string ContentType = "content-type";
        }
    }

    internal static class Outbox
    {
        internal const string PublisherJob = "outbox-publisher";
        internal const string PublisherTrigger = "outbox-publisher-trigger";
        internal const string ConfigurationNotFound = "Outbox processing configuration is missing.";
        internal const string BatchSizeMustBePositive = "Outbox batch size must be greater than zero.";
        internal const string PollingIntervalMustBePositive = "Outbox polling interval must be greater than zero.";
        internal const string RetryDelayMustBePositive = "Outbox retry delay must be greater than zero.";
        internal const string Table = "OutboxMessages";
        internal const string JsonbColumnType = "jsonb";
        internal const int TypeMaxLength = 200;
        internal const int PartitionKeyMaxLength = 200;
        internal const int LastErrorMaxLength = 2000;
        internal const string PendingIndex = "IX_OutboxMessages_Pending";
        internal const string PendingIndexFilter = "\"PublishedAtUtc\" IS NULL";
        internal const string VersionCheck = "CK_OutboxMessages_Version_Positive";
        internal const string VersionCheckSql = "\"Version\" > 0";
        internal const string AttemptsCheck = "CK_OutboxMessages_Attempts_NonNegative";
        internal const string AttemptsCheckSql = "\"Attempts\" >= 0";
        internal const string EmptyId = "Outbox message ID cannot be empty.";
        internal const string TimestampMustBeUtc = "Timestamp '{0}' must use the UTC offset.";
        internal const string TimestampBeforeOccurrence = "Timestamp '{0}' cannot be earlier than the event occurrence time.";
        internal const string PublishedMessageFailure = "A failure cannot be registered for an already published outbox message.";
    }

    internal static class Constraints
    {
        internal const string UsersEmailUnique = "UX_Users_Email";
        internal const string UsersPhoneNumberUnique = "UX_Users_PhoneNumber";
    }
}
