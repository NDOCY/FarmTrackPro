using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
//using FarmTrack.Models;

namespace FarmTrack.Controllers
{
    public class DashboardController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();
        // Admin Dashboard
        public ActionResult AdminDashboard()
        {
            // Check if the role is not set or if it's neither Admin nor Owner
            //if (Session["Role"] == null || (Session["Role"].ToString() != "Admin" && Session["Role"].ToString() != "Owner"))
            //{
              //  return RedirectToAction("Login", "Account");
            //}

            // Get the current user's ID from the session
            var currentUserId = Convert.ToInt32(Session["UserId"]);

            // Get all users
            var users = db.Users.ToList();

            // Get counts for livestock, inventory, jobs, and tasks
            var livestock = db.Livestocks.Count();
            var inventory = db.Inventories.Count();
            var jobs = db.Jobs.Count();

            // Tasks: Filter tasks based on the user's department if the user is an Admin
            /*List<FarmTask> tasks;
            if (Session["Role"].ToString() == "Admin")
            {
                // Get the department of the current admin user
                var department = db.Users.Where(u => u.UserId == currentUserId).Select(u => u.Department).FirstOrDefault();

                // Filter tasks based on the department (Admin sees tasks from their department)
                tasks = db.Tasks.Where(t => t.AssignedDepartment == department).ToList();
            }
            else
            {
                // Owners can see all tasks
                tasks = db.Tasks.ToList();
            }*/
            List<FarmTask> tasks;
            if (Session["Role"].ToString() == "Admin")
            {
                // Get the department of the current admin user
                var department = db.Users.Where(u => u.UserId == currentUserId).Select(u => u.Department).FirstOrDefault();

                // Admins only see tasks in their department that are NOT completed
                tasks = db.Tasks.Where(t => t.AssignedDepartment == department && t.Status != "Completed").ToList();
            }
            else
            {
                // Owners can see all active (non-completed) tasks
                tasks = db.Tasks.Where(t => t.Status != "Completed").ToList();
            }

            // Group the livestock by type and count the number of each type
            var livestockGroupedByType = db.Livestocks
                .GroupBy(l => l.Type)
                .Select(g => new LivestockGroupViewModel
                {
                    Type = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // Get the most recent activity logs
            var logs = db.ActivityLogs.OrderByDescending(log => log.Timestamp).ToList();



            // Pass data to the view using ViewBag
            ViewBag.Users = users;
            ViewBag.LivestockCount = livestock;
            ViewBag.InventoryCount = inventory;
            ViewBag.JobsCount = jobs;
            ViewBag.TasksCount = tasks.Count();
            ViewBag.LivestockGroupedByType = livestockGroupedByType;

            // Cast tasks to a list of FarmTask for correct usage in the view
            ViewBag.Tasks = tasks;
            //ViewBag.AllTasks = db.Tasks.ToList(); // Or any appropriate collection of FarmTask objects
            ViewBag.AllTasks = db.Tasks.Where(t => t.Status != "Completed").ToList(); // Ensure only active tasks are listed


            return View(logs);
        }

        public ActionResult UserDashboard()
        {
            // Get the current user ID
            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Find(userId);

            if (user == null)
            {
                return HttpNotFound();
            }

            // Tasks assigned directly to the user
            var userTasks = db.Tasks
                              .Where(t => t.AssignedTo == userId)
                              .OrderByDescending(t => t.DueDate)
                              .ToList();

            // Tasks assigned to user's department
            var departmentTasks = db.Tasks
                                    .Where(t => t.AssignedDepartment == user.Department)
                                    .OrderByDescending(t => t.DueDate)
                                    .ToList();

            // Merge both and remove duplicates
            var allTasks = userTasks.Concat(departmentTasks).Distinct().ToList();

            // All job listings
            var jobs = db.Jobs.ToList();

            // Count of job applications made by user
            int jobApplicationsCount = db.JobApplications.Count(a => a.UserId == userId);

            // Count of received messages (direct, admin/broadcast, or department-based)
            int messageCount = db.Messages.Count(m =>
                m.RecipientId == userId ||
                (m.IsToAdmins && (user.Role == "Admin" || user.Role == "Owner")) ||
                (m.IsGroupMessage && m.Department == user.Department)
            );

            // Create view model
            var dashboardViewModel = new UserDashboardViewModel
            {
                User = user,
                AssignedTasks = allTasks,
                Job = jobs,
                JobApplicationsCount = jobApplicationsCount,
                MessagesCount = messageCount
            };

            return View(dashboardViewModel);
        }





    }
}