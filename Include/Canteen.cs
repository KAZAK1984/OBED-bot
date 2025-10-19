namespace OBED.Include
{
    class Canteen(string name, int buildingNumber, int floor, string? description = null, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null) : BasePlace(name, description, reviews, menu, tegs), ILocatedUni
    {
		public int BuildingNumber { get; private set; } = buildingNumber;
		public int Floor { get; private set; } = floor;
	}
}
