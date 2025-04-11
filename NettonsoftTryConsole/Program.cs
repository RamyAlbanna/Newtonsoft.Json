using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
            Console.WriteLine(o.Name);
        }
    }

    internal class Movie
    {
        public string Name { get; set; }
        public string Age { get; set; }
        public string[] Genres { get; set; }
    }
}
