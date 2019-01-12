using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace SearchLib
{
    public class Repository
    {
        public static IEnumerable<Movie> GetMoviesFromFile(string movieJsonPath)
        {
            return JsonConvert.DeserializeObject<List<Movie>>(File.ReadAllText(movieJsonPath));
        }
    }

}
