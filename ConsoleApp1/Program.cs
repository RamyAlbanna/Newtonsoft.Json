

using System.Xml.Linq;

internal class Program
{
    private static void Main(string[] args)
    {
        List<Product> ProductsList = [

            new()
            {
                    Id = 1,
                    Name = "Product 1"
            },
            new()
            {
                    Id = 2,
                    Name = "Product 2"
            },
        ];

        ShowProducts(ProductsList);
    }

    static void ShowProducts(List<Product> ProductsList)
    {
        foreach (Product product in ProductsList)
        {
            Console.WriteLine(product.Name);
        }
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
}