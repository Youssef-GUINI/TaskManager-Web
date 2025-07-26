using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace TaskManager.Controllers
{
	public class TestController : Controller
	{
		private readonly IEmailSender _emailSender;
		public TestController(IEmailSender emailSender)
		{
			_emailSender = emailSender;
		}

		[HttpGet("/test-email")]
		public async Task<IActionResult> SendTest()
		{
			await _emailSender.SendEmailAsync("tonemail@gmail.com", "Test Email", "<p>Ça marche !</p>");
			return Ok("Email envoyé !");
		}
	}
}

