namespace OBED.Include
{
    class Canteen : BasePlace, ILocatedUni
    {
        public int BuildingNumber { get; private set; }
        public int Floor { get; private set; }

#pragma warning disable IDE0290 // Использовать основной конструктор
		public Canteen(string name, int buildingNumber, int floor, string? description = null, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null) : base(name, description, reviews, menu, tegs)
#pragma warning restore IDE0290 // Использовать основной конструктор
		{
            BuildingNumber = buildingNumber;
            Floor = floor;

            //ObjectLists.Canteens.Add(this);
        }
    }
}
