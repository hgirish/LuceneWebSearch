using System.IO;
using LuceneWebSearch.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SearchLib;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LuceneWebSearch.Controllers
{
    public class SearchController : Controller
    {
        SearchEngine engine;
        string indexLocation;
        public SearchController(IConfiguration configuration)
        {
            indexLocation = configuration["SearchIndexLocation"];
            if (!Directory.Exists(indexLocation))
            {
                Directory.CreateDirectory(indexLocation);
            }
           // DeleteIndexFiles();
             
            engine = new SearchEngine(indexLocation);
            
          //  engine.BuildIndex();
        }
        // GET: /<controller>/
        public IActionResult Index(string search)
        {
            SearchResults results = new SearchResults();
            if (!string.IsNullOrEmpty(search))
            {
                 results = engine.Search(search);
            }
            SearchIndexModel model = new SearchIndexModel
            {
                Results = results,
                Search = search
            };
            return View(model);
        }
        public IActionResult BuildIndex()
        {
           // engine = new SearchEngine(indexLocation);

            engine.BuildIndex();
            return RedirectToAction("Index");

        }
        public IActionResult AddMovie()
        {
            Movie movie = new Movie
            {
                Title = "Bodyguard",
                Description = "A new suspense thriller",
                MovieId = 9999,
                Rating = "R"

            };
            engine.UpdateMovie(movie);
            return RedirectToAction("Index", new { Search = "Bodygurard" });
        }
        public IActionResult DeleteIndexes()
        {
            DeleteIndexFiles();
            return RedirectToAction("Index");
        }
        private  void DeleteIndexFiles()
        {
            foreach (FileInfo f in new DirectoryInfo(indexLocation)?.GetFiles())
            {
                try
                {
                    f?.Delete();
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine(ex.Message);
                 
                }
            }
        }


    }
}
