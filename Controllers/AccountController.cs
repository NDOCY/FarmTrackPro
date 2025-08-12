using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
//using FarmTrack.Models;
using FarmTrack.Models;

namespace FarmTrack.Controllers
{
    public class AccountController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        // GET: Account
        public ActionResult Index(string search)
        {
            var users = db.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.PhoneNumber.Contains(search) ||
                    u.Department.Contains(search) ||
                    u.Role.Contains(search));
            }

            ViewBag.SearchQuery = search;
            return View(users.ToList());
        }

        // GET: Account/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            User user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // POST: Account/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UserId,FullName,Email,PasswordHash,PhoneNumber,Address,ProfilePictureUrl,DateRegistered,Department,IsAdmin")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Users.Add(user);
                db.SaveChanges();

                int userId = Convert.ToInt32(Session["UserId"]);
                db.LogActivity(userId, $"Created a new user: {user.FullName} ({user.Email})");

                return RedirectToAction("Index");
            }

            return View(user);
        }

        // GET: Account/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            User user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // POST: Account/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "UserId,FullName,Email,PasswordHash,PhoneNumber,Address,ProfilePictureUrl,DateRegistered,Department,Role")] User user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = db.Users.Find(user.UserId);
                if (existingUser != null)
                {
                    existingUser.FullName = user.FullName;
                    existingUser.Email = user.Email;
                    existingUser.PasswordHash = user.PasswordHash;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.Address = user.Address;
                    existingUser.ProfilePictureUrl = user.ProfilePictureUrl;
                    existingUser.Department = user.Department;
                    existingUser.Role = user.Role;

                    db.Entry(existingUser).State = EntityState.Modified;
                    db.SaveChanges();

                    int userId = Convert.ToInt32(Session["UserId"]);
                    db.LogActivity(userId, $"Edited user: {existingUser.FullName} ({existingUser.Email})");
                }

                return RedirectToAction("Index");
            }

            return View(user);
        }

        // GET: Account/Delete/5
        public ActionResult Delete(int? id)
        {
            string currentUserRole = Session["Role"]?.ToString();

            if (currentUserRole != "Owner")
                return RedirectToAction("AdminDashboard", "Dashboard");

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            User user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // POST: Account/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            User user = db.Users.Find(id);
            if (user != null)
            {
                db.Users.Remove(user);
                db.SaveChanges();

                int userId = Convert.ToInt32(Session["UserId"]);
                db.LogActivity(userId, $"Deleted user: {user.FullName} ({user.Email})");
            }

            return RedirectToAction("Index");
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        public ActionResult Register(User model)
        {
            var existingUser = db.Users.SingleOrDefault(u => u.Email == model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email is already taken. Please choose another.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);
                model.DateRegistered = DateTime.UtcNow;
                model.Role = "User";

                db.Users.Add(model);
                db.SaveChanges();

                //db.LogActivity(model.UserId, $"Registered a new account: {model.FullName} ({model.Email})");

                TempData["SuccessMessage"] = "Your registration has been successful.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            var user = db.Users.SingleOrDefault(u => u.Email == email);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                Session["UserId"] = user.UserId;
                Session["FullName"] = user.FullName;
                Session["Role"] = user.Role;
                Session["Department"] = user.Department;

                db.LogActivity(user.UserId, $"Logged in: {user.FullName} ({user.Email})");

                if (user.Role == "Admin" || user.Role == "Owner")
                    return RedirectToAction("AdminDashboard", "Dashboard");

                return RedirectToAction("UserDashboard", "Dashboard");
            }

            ModelState.AddModelError("", "Invalid login credentials.");
            return View();
        }

        // Logout
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        // GET: Account/UserProfile
        public ActionResult UserProfile()
        {
            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Find(userId);

            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // POST: Account/UserProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserProfile(User model)
        {
            int userId = Convert.ToInt32(Session["UserId"]);
            var existingUser = db.Users.Find(userId);

            if (existingUser == null)
                return HttpNotFound();

            if (ModelState.IsValid)
            {
                existingUser.FullName = model.FullName;
                existingUser.PhoneNumber = model.PhoneNumber;
                existingUser.Address = model.Address;

                if (!string.IsNullOrWhiteSpace(model.PasswordHash))
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);

                db.SaveChanges();

                db.LogActivity(userId, $"Updated profile: {existingUser.FullName}");

                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction("UserProfile");
            }

            return View(existingUser);
        }
    }
}
