using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Models;
using TaskManager.data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using MimeKit;
using MailKit.Net.Smtp;

namespace TaskManager.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<User> userManager,
                                 SignInManager<User> signInManager,
                                 IEmailSender emailSender,
                                 ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (!model.Email.EndsWith("@sqli.com", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(string.Empty, "Seuls les emails @sqli.com sont autorisés.");
                    return View(model);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Utilisateur introuvable.");
                    return View(model);
                }

                // Vérifier le mot de passe avec CheckPasswordSignInAsync
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // Connexion effective
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Workspace");
                }

                ModelState.AddModelError(string.Empty, "Tentative de connexion invalide.");
                return View(model);
            }

            return View(model);
        }



        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Recherche utilisateur via recoveryEmail (pas Email principal)
            var user = await _context.Users.SingleOrDefaultAsync(u => u.RecoveryEmail == model.Email);

            if (user == null)
            {
                // Ne pas révéler si email inconnu (sécurité)
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // Génération du token de reset
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Création du lien de reset avec l'email de récupération (model.Email)
            var resetLink = Url.Action("ResetPassword", "Account", new { token, email = model.Email }, Request.Scheme);

            // Envoi du mail à l'email de récupération
            // await _emailSender.SendEmailAsync(model.Email, "Reset Password",
            //     $"Please reset your password by clicking here: <a href='{resetLink}'>link</a>");
            await SendPasswordResetEmail(model.Email, resetLink);
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
                return BadRequest("Invalid password reset token.");

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        // // POST: /Account/ResetPassword
        // [HttpPost]
        // public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        // {
        //     if (!ModelState.IsValid)
        //         return View(model);

        //     // Chercher utilisateur via recoveryEmail (model.Email ici est l'email de récupération)
        //     var user = await _context.Users.SingleOrDefaultAsync(u => u.RecoveryEmail == model.Email);

        //     if (user == null)
        //         return RedirectToAction("ResetPasswordConfirmation");

        //     var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        //     if (result.Succeeded)
        //         return RedirectToAction("ResetPasswordConfirmation");

        //     foreach (var error in result.Errors)
        //         ModelState.AddModelError(string.Empty, error.Description);

        //     return View(model);
        // }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return RedirectToAction("ForgotPasswordConfirmation");

            // Générer token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Générer lien complet avec token et email encodé
            var resetLink = Url.Action("ResetPassword", "Account", new { token = token, email = model.Email }, Request.Scheme);

            // Envoi de l'email personnalisé ici
            await SendPasswordResetEmail(model.Email, resetLink);

            return RedirectToAction("ForgotPasswordConfirmation");
        }


        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }


public async Task SendPasswordResetEmail(string recipientEmail, string resetLink)
{
    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("Support SQLI", "bouchikhidoha2@gmail.com")); // Titre dans boîte
    message.To.Add(MailboxAddress.Parse(recipientEmail));
    message.Subject = "Réinitialisation de votre mot de passe";

    var bodyBuilder = new BodyBuilder();

    // Contenu HTML stylisé
    bodyBuilder.HtmlBody = $@"
<html>
  <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
    <div style='max-width: 600px; margin: auto; background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
      <h2 style='color: #333333;'>Réinitialisation de votre mot de passe</h2>
      <p>Bonjour,</p>
      <p>Vous avez demandé à réinitialiser votre mot de passe. Cliquez sur le bouton ci-dessous pour procéder :</p>
      <p style='text-align: center; margin: 40px 0;'>
        <a href='{resetLink}' style='background-color: #007BFF; color: white; padding: 15px 25px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>Réinitialiser le mot de passe</a>
      </p>
      <p>Si vous n’avez pas fait cette demande, vous pouvez ignorer ce message en toute sécurité.</p>
      <p>— L’équipe Support SQLI_Taches</p>
    </div>
  </body>
</html>";

    message.Body = bodyBuilder.ToMessageBody();

    using var client = new SmtpClient();
    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
    await client.AuthenticateAsync("bouchikhidoha2@gmail.com", "bciyztvfsgdcgppw");
    await client.SendAsync(message);
    await client.DisconnectAsync(true);
}


    }
}



