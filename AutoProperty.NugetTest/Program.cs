using System.Reflection;
using AutoProperty.Generator;
using AutoProperty.NugetTest;

[assembly: AutoPropertyAttribute(typeof(IModel))]

namespace AutoProperty.NugetTest
{
    public static class Program
    {
        public static void Main()
        {
            var model = new Models()
            {
                Id = 2
            };
            Console.WriteLine(model);
        }
    }


    public interface IModel
    {
        public int Id { get; set; }
    }

    public partial class Models : IModel
    {
    }
}