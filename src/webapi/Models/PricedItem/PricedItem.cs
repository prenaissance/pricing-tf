namespace PricingTf.WebApi.Models.PricedItem;

public class PricedItem
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ListingDetails? BuyListing { get; set; } = default!;
    public IList<ListingDetails> BuyListings { get; set; } = [];
    public ListingDetails? SellListing { get; set; } = default!;
    public IList<ListingDetails> SellListings { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}