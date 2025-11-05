namespace event_finder.Domain;

public class EventResponse
{
    public List<TechEvent> Events { get; set; } = new();
    public List<ReasoningStep> ReasoningSteps { get; set; } = new();
    public int TotalTokens { get; set; }
}
