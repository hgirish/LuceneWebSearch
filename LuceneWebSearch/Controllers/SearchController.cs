using System;
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
        //readonly string _indexLocation;
        private readonly ISearchEngine _engine;

        public SearchController(IConfiguration configuration, ISearchEngine engine)
        {
            _engine = engine;
            // indexLocation = configuration["SearchIndexLocation"];
            // if (!Directory.Exists(indexLocation))
            // {
            //     Directory.CreateDirectory(indexLocation);
            // }
            //// DeleteIndexFiles();

            // engine = new SearchEngine(indexLocation);

            //  engine.BuildIndex();
        }
   
        // GET: /<controller>/
        public IActionResult Index(string search)
        {
            SearchResults results = new SearchResults();
            if (!string.IsNullOrEmpty(search))
            {
                 results = _engine.Search(search);
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

            _engine.BuildIndex();
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
            _engine.UpdateMovie(movie);
            return RedirectToAction("Index", new { Search = "Bodygurard" });
        }
        //public IActionResult DeleteIndexes()
        //{
        //    DeleteIndexFiles();
        //    return RedirectToAction("Index");
        //}

        public ActionResult ClearIndex()
        {
            if (_engine.ClearLuceneIndex())
                TempData["Result"] = "Search index was cleared successfully!";
            else
                TempData["ResultFail"] = "Index is locked and cannot be cleared, try again later or clear manually!";
            return RedirectToAction("Index");
        }

        public ActionResult ClearIndexRecord(int id)
        {
            _engine.ClearLuceneIndexRecord(id);
            TempData["Result"] = "Search index record was deleted successfully!";
            return RedirectToAction("Index");
        }
        //private  void DeleteIndexFiles()
        //{
        //    foreach (FileInfo f in new DirectoryInfo(_indexLocation)?.GetFiles())
        //    {
        //        try
        //        {
        //            f?.Delete();
        //        }
        //        catch (System.Exception ex)
        //        {
        //            System.Console.WriteLine(ex.Message);
                 
        //        }
        //    }
        //}


    }
}
