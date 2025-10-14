namespace OBED.Include
{
    public enum ProductType
    {
        MainDish,
        SideDish,
        Drink,
        Appetizer
    }

    class Product(string name, ProductType type, (float value, bool perGram) price)
    {
        public string Name { get; init; } = name;
        public (float value, bool perGram) Price { get; private set; } = price;
        public ProductType Type { get; init; } = type;

        // TODO: SetPrice с процеркой на тип учётки
    }
}
