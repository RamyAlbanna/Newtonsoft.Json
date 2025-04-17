using System;
using SharedNewtonsoft.Json;

namespace NettonsoftTryConsole
{
    internal class Program
    {
        static void Main()
        {
            string movieJson = @"{
              'Name': 'Bad Boys',
              'Age': '1995-4-7T00:00:00',
              'Genres': [
                'Action',
                'Comedy'
              ]
            }";
            Movie o = JsonConvert.DeserializeObject<Movie>(movieJson);
            Console.WriteLine(o.Age);
        }
    }

    internal class Movie
    {
        public string Name { get; set; }
        public string Age { get; set; }
        public string[] Genres { get; set; }
    }
}
