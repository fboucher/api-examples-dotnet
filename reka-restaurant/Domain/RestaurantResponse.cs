using System;

namespace web.Domain;

public class RestaurantResponse
{
    public List<Restaurant> Restaurants { get; set; } = new();
    public List<ReasoningStep> ReasoningSteps { get; set; } = new();
    public int TotalTokens { get; set; }

}
