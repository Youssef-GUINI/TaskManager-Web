using Microsoft.AspNetCore.Mvc;
using TaskManager.Data;
using TaskManager.Models;
using Microsoft.EntityFrameworkCore;
namespace TaskManager.Controllers
{
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context; //Tu déclares un champ _context qui sert à accéder à la base de données.

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index() //Elle récupère toutes les lignes de la table Tasks depuis la base.


        {
            var tasks = await _context.Tasks.ToListAsync(); // récupère les tâches de la base
            return View(tasks); // les envoie à la vue Index.cshtml
        }
        [HttpGet]
        public IActionResult Create() //Cette méthode est appelée quand l’utilisateur clique sur "Nouvelle tâche"
        {
            return View(); // Affiche le formulaire vide
        }

        [HttpPost]
        public IActionResult Create(TaskModel task) //[HttpPost] : cette méthode est déclenchée quand l’utilisateur soumet le formulaire.


        {
            if (ModelState.IsValid)
            {
                _context.Tasks.Add(task); // Ajoute la tâche dans la base
                _context.SaveChanges();   // Sauvegarde en base
                return RedirectToAction("Index"); // Revient à la liste des tâches
            }
            return View(task); // Si erreur de saisie, réaffiche le formulaire avec erreurs
        }


    }
}
