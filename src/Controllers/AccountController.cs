﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Phytime.ViewModels;
using Phytime.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;

namespace Phytime.Controllers
{
    public class AccountController : Controller
    {
        private readonly IRepository _repository;

        public AccountController(IConfiguration config, IRepository repository = null)
        {
            _repository = repository ?? new PhytimeRepository(config);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await _repository.GetUserAsync(model.Email, model.Password);
                if (user != null)
                {
                    await Authenticate(model.Email);

                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Incorrect login and(or) password");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await _repository.GetUserByEmailAsync(model.Email);
                if (user == null)
                {
                    var newUser = new User() { Email = model.Email };
                    newUser.SetPassword(model.Password);
                    _repository.Add(newUser);

                    await Authenticate(model.Email);

                    return RedirectToAction("Index", "Home");
                }
                else
                    ModelState.AddModelError("", "Incorrect login and(or) password");
            }
            return View(model);
        }

        private async Task Authenticate(string userName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, userName)
            };
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        protected override void Dispose(bool disposing)
        {
            (_repository as PhytimeRepository).Dispose();
            base.Dispose(disposing);
        }
    }
}
