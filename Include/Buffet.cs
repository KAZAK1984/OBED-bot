namespace OBED.Include
{
    class Buffet : BasePlace, ILocatedUni
    {
        public int BuildingNumber { get; private set; }
        public int Floor { get; private set; }
        
        public static List<Buffet> All { get; private set; } = [];

        public Buffet(string name, int buildingNumber, int floor, string? description = null, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null) : base(name, description, reviews, menu, tegs)
        {
            BuildingNumber = buildingNumber;
            Floor = floor;

            All.Add(this);
        }
    }
}
