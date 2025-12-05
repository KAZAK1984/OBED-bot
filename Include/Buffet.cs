namespace OBED.Include
{
    class Buffet(long placeid,string name, int buildingNumber, int floor, string? description = null, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null) : BasePlace(placeid, name, description, reviews, menu, tegs), ILocatedUni
    {
		public int BuildingNumber { get; private set; } = buildingNumber;
		public int Floor { get; private set; } = floor;
	}
}
