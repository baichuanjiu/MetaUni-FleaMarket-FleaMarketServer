namespace Market.API.ReusableClass
{
    public class PriceRange 
    {
        public PriceRange()
        {
        }

        public PriceRange(double start, double end)
        {
            Start = start;
            End = end;
        }

        public double Start { get; set; }
        public double End { get; set; }
    }

    public class PriceData
    {
        public PriceData()
        {
        }

        public PriceData(string type, double? price, PriceRange? priceRange)
        {
            Type = type;
            Price = price;
            PriceRange = priceRange;
        }

        public string Type { get; set; } // pending（待定） accurate（准确价格） range（价格范围）
        public double? Price { get; set; }
        public PriceRange? PriceRange { get; set; }
    }
}
