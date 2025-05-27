using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FarmTrack.Models;
using FarmTrack.Services;

namespace FarmTrack.Controllers
{
    public class JobsController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        // GET: Jobs
        public ActionResult Index(string search)
        {
            var jobs = db.Jobs.Include(j => j.User);

            // Apply search filter if a search query is provided
            if (!string.IsNullOrEmpty(search))
            {
                jobs = jobs.Where(j =>
                    j.Title.Contains(search) ||
                    j.Description.Contains(search) ||
                    j.JobType.Contains(search));
            }

            ViewBag.SearchQuery = search; // Preserve search term in view
            return View(jobs.ToList());
        }

        // GET: Jobs/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Job job = db.Jobs.Find(id);
            if (job == null)
            {
                return HttpNotFound();
            }
            return View(job);
        }

        // GET: Jobs/Create
        public ActionResult Create()
        {
            string currentUserRole = Session["Role"]?.ToString();

            if (currentUserRole != "Owner")
            {
                return RedirectToAction("AdminDashboard", "Dashboard");
            }

            ViewBag.UserId = new SelectList(db.Users, "UserId", "FullName");
            return View();
        }

        // POST: Jobs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "JobId,Title,Description,JobType,Location,EmploymentType,DatePosted,ApplicationDeadline,SalaryRange,RequiredSkills")] Job job)
        {
            if (ModelState.IsValid)
            {
                if (Session["UserId"] != null)
                {
                    job.UserId = (int)Session["UserId"];
                }
                else
                {
                    ModelState.AddModelError("", "User is not logged in.");
                    return View(job);
                }

                bool userExists = db.Users.Any(u => u.UserId == job.UserId);
                if (!userExists)
                {
                    ModelState.AddModelError("", "User does not exist.");
                    return View(job);
                }

                job.DatePosted = DateTime.UtcNow;
                db.Jobs.Add(job);
                db.SaveChanges();

                TempData["JobMessage"] = "Job created successfully!";

                // Log activity
                db.LogActivity(job.UserId, $"Created job: {job.Title} (ID: {job.JobId})");
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(job);
        }

        // GET: Jobs/Edit/5
        public ActionResult Edit(int? id)
        {
            string currentUserRole = Session["Role"]?.ToString();

            if (currentUserRole != "Owner")
            {
                return RedirectToAction("AdminDashboard", "Dashboard");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Job job = db.Jobs.Find(id);
            if (job == null)
            {
                return HttpNotFound();
            }

            ViewBag.UserId = new SelectList(db.Users, "UserId", "FullName", job.UserId);
            return View(job);
        }

        // POST: Jobs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "JobId,Title,Description,JobType,DatePosted")] Job job)
        {
            if (ModelState.IsValid)
            {
                var existingJob = db.Jobs.Find(job.JobId);
                if (existingJob == null)
                {
                    return HttpNotFound();
                }

                if (Session["UserId"] == null || (int)Session["UserId"] != existingJob.UserId)
                {
                    ModelState.AddModelError("", "You are not authorized to edit this job.");
                    return View(job);
                }

                existingJob.Title = job.Title;
                existingJob.Description = job.Description;
                existingJob.JobType = job.JobType;
                existingJob.DatePosted = job.DatePosted;

                db.SaveChanges();

                // Log activity
                db.LogActivity(existingJob.UserId, $"Edited job: {existingJob.Title} (ID: {existingJob.JobId})");
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(job);
        }

        // GET: Jobs/Delete/5
        public ActionResult Delete(int? id)
        {
            string currentUserRole = Session["Role"]?.ToString();

            if (currentUserRole != "Owner")
            {
                return RedirectToAction("AdminDashboard", "Dashboard");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Job job = db.Jobs.Find(id);
            if (job == null)
            {
                return HttpNotFound();
            }

            return View(job);
        }

        // POST: Jobs/Delete/5
        /*[HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Job job = db.Jobs.Find(id);
            if (job != null)
            {
                string jobTitle = job.Title;
                db.Jobs.Remove(job);
                db.SaveChanges();

                // Log activity
                db.LogActivity(Convert.ToInt32(Session["UserId"]), $"Deleted job: {jobTitle} (ID: {id})");
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        */
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Job job = db.Jobs.Include(j => j.JobApplications).FirstOrDefault(j => j.JobId == id);

            if (job != null)
            {
                string jobTitle = job.Title;

                // First, delete all related job applications
                db.JobApplications.RemoveRange(job.JobApplications);
                db.SaveChanges();

                // Now delete the job itself
                db.Jobs.Remove(job);
                db.SaveChanges();

                // Log activity
                db.LogActivity(Convert.ToInt32(Session["UserId"]), $"Deleted job: {jobTitle} (ID: {id})");
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // GET: Apply for Job
        public ActionResult Apply(int jobId)
        {
            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Find(userId);
            var job = db.Jobs.Find(jobId);

            if (job == null)
                return HttpNotFound();

            var model = new JobApplication
            {
                JobId = jobId,
                Job = job,
                UserId = userId,
                User = user,
                PhoneNumber = user?.PhoneNumber,
                Address = user?.Address,
                ID = user?.ID,
                CV = user?.CV
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SubmitApplication(JobApplication application, HttpPostedFileBase CVUpload, HttpPostedFileBase IDUpload)
        {
            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Find(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            var job = db.Jobs.Find(application.JobId);
            if (job == null) return RedirectToAction("Index", "Jobs");

            if (string.IsNullOrEmpty(user.PhoneNumber)) user.PhoneNumber = application.PhoneNumber;
            if (string.IsNullOrEmpty(user.Address)) user.Address = application.Address;

            var connectionString = ConfigurationManager.AppSettings["AzureBlobConnection"];
            var blobService = new BlobService(connectionString);

            if (CVUpload != null && CVUpload.ContentLength > 0)
            {
                string cvUrl = await blobService.UploadFileAsync(CVUpload);
                application.CV = cvUrl;
                user.CV = cvUrl;
            }

            if (IDUpload != null && IDUpload.ContentLength > 0)
            {
                string idUrl = await blobService.UploadFileAsync(IDUpload);
                application.ID = idUrl;
                user.ID = idUrl;
            }

            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();

            application.UserId = userId;
            application.AppliedDate = DateTime.Now;
            db.JobApplications.Add(application);
            db.SaveChanges();

            db.LogActivity(userId, $"Applied for job: {job.Title} (ID: {job.JobId})");
            db.SaveChanges();

            TempData["Message"] = "Application submitted successfully!";
            return RedirectToAction("UserDashboard", "Dashboard");
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


        // GET: JobApplications
        public ActionResult JobApplications(string search)
        {
            int userId = Convert.ToInt32(Session["UserId"]); // Get logged-in user ID

            // Check if the user is an "Owner"
            var user = db.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null || user.Role != "Owner")
            {
                return RedirectToAction("AdminDashboard", "Dashboard"); // Redirect non-owners
            }

            // Fetch all job applications with user and job details
            var applications = db.JobApplications
                                 .Include(j => j.Job)
                                 .Include(j => j.User)
                                 .AsQueryable();

            // Apply search filter if a query is provided
            if (!string.IsNullOrEmpty(search))
            {
                applications = applications.Where(app =>
                    app.User.FullName.Contains(search) ||
                    app.User.Email.Contains(search) ||
                    app.Job.Title.Contains(search) ||
                    app.AppliedDate.ToString().Contains(search));
            }

            // Pass search term back to the view for persistence
            ViewBag.SearchQuery = search;

            return View(applications.ToList()); // ✅ Return only filtered applications
        }

        // GET: Review Job Application
        public ActionResult ReviewApplication(int id)
        {
            var application = db.JobApplications
                .Include("User")
                .Include("Job")
                .FirstOrDefault(a => a.JobApplicationId == id);

            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReviewApplication(int id, string status, string notes, DateTime? interviewDate, string interviewVenue, string interviewerName)
        {
            var application = db.JobApplications
                .Include("Job")
                .Include("User")
                .FirstOrDefault(a => a.JobApplicationId == id);

            if (application == null)
            {
                TempData["Error"] = "Application not found.";
                return RedirectToAction("ApplicationsList");
            }

            application.Status = status;
            application.ReviewNotes = notes;

            // Save interview details if provided
            application.InterviewDate = interviewDate;
            application.InterviewVenue = interviewVenue;
            application.InterviewerName = interviewerName;

            // Send email only if interview is being scheduled and not already sent
            if (status == "Accepted" && interviewDate.HasValue && !application.InterviewEmailSent)
            {
                try
                {
                    SendInterviewEmail(application);
                    application.InterviewEmailSent = true;
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Application updated but email failed: " + ex.Message;
                }
            }

            db.SaveChanges();

            TempData["Message"] = $"Application {status.ToLower()} successfully.";
            return RedirectToAction("JobApplications");
        }

        private void SendInterviewEmail(JobApplication app)
        {
            var toEmail = app.User.Email;
            var subject = $"Interview Scheduled - {app.Job.Title}";
            var body = $@"
                Dear {app.User.FullName},

                Congratulations! You have been shortlisted for the position of {app.Job.Title}.

                Your interview is scheduled as follows:

                📅 Date: {app.InterviewDate?.ToString("dddd, dd MMMM yyyy hh:mm tt")}
                📍 Venue: {app.InterviewVenue}
                👤 Interviewer: {app.InterviewerName}

                Please arrive at least 10 minutes early and bring any necessary documents.

                Best of luck!

                Kind regards,
                FarmTrack Hiring Team
            ";

            var message = new MailMessage();
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = false;
            message.From = new MailAddress("as.nkab01@gmail.com");

            using (var smtp = new SmtpClient("smtp.gmail.com", 587))
            {
                smtp.Credentials = new NetworkCredential("as.nkab01@gmail.com", "vmbqzurtzekhxjcv");
                smtp.EnableSsl = true;
                smtp.Send(message);
            }
        }





        // Your controller needs to properly include the Job entity
        public ActionResult MyApplication()
        {
            int userId = Convert.ToInt32(Session["UserId"]);
            var applications = db.JobApplications
                .Include(a => a.Job) // Use lambda expression instead of string
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppliedDate)
                .ToList();
            return View(applications);
        }
        // GET: JobApplication/Details/5
        public ActionResult Detail(int id)
        {
            // Find the job application by ID, include related Job and User details
            var application = db.JobApplications
                .Include("Job")  // Include the Job details (e.g., title)
                .Include("User") // Include the User details (e.g., name, email, phone)
                .FirstOrDefault(a => a.JobApplicationId == id);

            if (application == null)
            {
                return HttpNotFound(); // If the application doesn't exist, return 404
            }

            return View(application); // Pass the application object to the view
        }


    }
}
