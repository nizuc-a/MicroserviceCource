namespace Shared.Domain.Settings;

public class KafkaSettings
{
    public const string SectionName = "Kafka";
    public string BootstrapServers { get; set; }
}