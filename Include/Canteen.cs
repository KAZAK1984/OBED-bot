namespace OBED.Include
{
    class Canteen : BasePlace, ILocatedUni
    {
        public int BuildingNumber { get; private set; }
        public int Floor { get; private set; }

        /// <summary>
        /// Initializes a new Canteen with the specified name, building number, floor, and optional description, reviews, menu, and tags, and registers it in the global canteen list.
        /// </summary>
        /// <param name="name">Display name of the canteen.</param>
        /// <param name="buildingNumber">The building number where the canteen is located.</param>
        /// <param name="floor">The floor number where the canteen is located.</param>
        /// <param name="description">Optional descriptive text for the canteen.</param>
        /// <param name="reviews">Optional initial list of reviews for the canteen.</param>
        /// <param name="menu">Optional initial menu of products available at the canteen.</param>
        /// <param name="tegs">Optional list of tags associated with the canteen.</param>
        public Canteen(string name, int buildingNumber, int floor, string? description = null, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null) : base(name, description, reviews, menu, tegs)
        {
            BuildingNumber = buildingNumber;
            Floor = floor;

            ObjectLists.Canteens.Add(this);
        }
    }
}