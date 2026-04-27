namespace WiSave.Framework.EventSourcing.Kurrent.Configuration;

public sealed class KurrentForwarderOptions
{
    public string GroupName { get; set; } = "kurrent-forwarder";
    public bool FromStartWhenCreated { get; set; } = true;
    public int MaxSubscriberCount { get; set; } = 1;
    public string ConsumerStrategyName { get; set; } = "DispatchToSingle";
    public string[] StreamPrefixes { get; set; } = [];
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int MaxReconnectDelaySeconds { get; set; } = 30;
}
