
using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FarmTrack.Controllers
{
    public class TaskController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        // GET: Task List (Admin & Users)
        public ActionResult Index(string department, string status)
        {
            // Start with all tasks
            var tasks = db.Tasks.AsQueryable();

            // Apply department filter if provided
            if (!string.IsNullOrEmpty(department))
            {
                tasks = tasks.Where(t => t.AssignedDepartment == department);
            }

            // Apply status filter if provided
            if (!string.IsNullOrEmpty(status))
            {
                tasks = tasks.Where(t => t.Status == status);
            }

            // Fetch the filtered tasks
            //var taskList = await tasks.ToListAsync();
            var taskList = tasks.ToList(); // sync version




            // Set up Statuses dropdown with SelectList
            ViewBag.Statuses = new SelectList(new[]
            {
        new { Value = "", Text = "All" },
        new { Value = "Pending", Text = "Pending" },
        new { Value = "InProgress", Text = "In Progress" },
        new { Value = "Completed", Text = "Completed" }
    }, "Value", "Text");

            // Set up Departments dropdown based on tasks
            ViewBag.Departments = new SelectList(db.Tasks
                .Where(t => !string.IsNullOrEmpty(t.AssignedDepartment))
                .Select(t => t.AssignedDepartment)
                .Distinct()
                .ToList());

            return View(taskList); // Return the filtered tasks or all tasks if no filter is applied
        }

        // GET: Create Task
        public ActionResult Create()
        {
            // Status Dropdown
            ViewBag.Status = new List<SelectListItem>
    {
        new SelectListItem { Value = "", Text = "Select Status" },
        new SelectListItem { Value = "Pending", Text = "Pending" },
        new SelectListItem { Value = "InProgress", Text = "In Progress" },
        new SelectListItem { Value = "Completed", Text = "Completed" }
    };

            // Users Dropdown
            ViewBag.Users = new SelectList(db.Users.OrderBy(u => u.FullName), "UserId", "FullName");

            // Departments Dropdown
            ViewBag.Departments = new SelectList(
                db.Users
                  .Where(u => !string.IsNullOrEmpty(u.Department))
                  .Select(u => new { Value = u.Department, Text = u.Department })
                  .Distinct()
                  .ToList(),
                "Value", "Text"
            );

            // Recurrence Type Dropdown
            ViewBag.RecurrenceTypes = new SelectList(new[]
            {
        new { Value = "", Text = "Select Recurrence" },
        new { Value = "Daily", Text = "Daily" },
        new { Value = "Weekly", Text = "Weekly" },
        new { Value = "Monthly", Text = "Monthly" }
    }, "Value", "Text");

            return View();
        }

        // POST: Create Task
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FarmTask model)
        {
            if (ModelState.IsValid)
            {
                if (model.AssignedTo == null && string.IsNullOrEmpty(model.AssignedDepartment))
                {
                    ModelState.AddModelError("", "Task must be assigned to a user or department.");
                }
                else
                {
                    // Validate if AssignedTo belongs to the selected department
                    if (model.AssignedTo.HasValue)
                    {
                        var assignedUserDepartment = db.Users
                            .Where(u => u.UserId == model.AssignedTo.Value)
                            .Select(u => u.Department)
                            .FirstOrDefault() ?? "";  // Fix null issue

                        if (!string.IsNullOrEmpty(model.AssignedDepartment) && assignedUserDepartment != model.AssignedDepartment)
                        {
                            ModelState.AddModelError("", "Assigned user does not belong to the selected department.");
                        }
                    }

                    if (!ModelState.Values.Any(v => v.Errors.Count > 0)) // Ensure no errors before saving
                    {
                        try
                        {
                            db.Tasks.Add(model);
                            db.SaveChanges();
                            TempData["CreateMessage"] = "Task created successfully.";

                            // Log Activity
                            int userId = Convert.ToInt32(Session["UserId"]);
                            db.LogActivity(userId, $"Created a new task: {model.Title}");

                            // Notify Assigned User
                            if (model.AssignedTo.HasValue)
                            {
                                NotifyUser(model.AssignedTo.Value, "You have been assigned a new task: " + model.Title);
                            }

                            return RedirectToAction("Index");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error: " + ex.Message);
                            ModelState.AddModelError("", "Something went wrong while saving. Please try again.");
                        }
                    }
                }
            }

            // Re-populate dropdowns if ModelState is invalid
            ViewBag.Status = new List<SelectListItem>
    {
        new SelectListItem { Value = "", Text = "Select Status" },
        new SelectListItem { Value = "Pending", Text = "Pending" },
        new SelectListItem { Value = "InProgress", Text = "In Progress" },
        new SelectListItem { Value = "Completed", Text = "Completed" }
    };

            ViewBag.Users = new SelectList(db.Users.OrderBy(u => u.FullName), "UserId", "FullName");

            ViewBag.Departments = new SelectList(
                db.Users
                  .Where(u => !string.IsNullOrEmpty(u.Department))
                  .Select(u => new { Value = u.Department, Text = u.Department })
                  .Distinct()
                  .ToList(),
                "Value", "Text"
            );

            ViewBag.RecurrenceTypes = new SelectList(new[]
            {
        new { Value = "", Text = "Select Recurrence" },
        new { Value = "Daily", Text = "Daily" },
        new { Value = "Weekly", Text = "Weekly" },
        new { Value = "Monthly", Text = "Monthly" }
    }, "Value", "Text");

            return View(model);
        }



        // POST: Update Task Status
        [HttpPost]
        public ActionResult UpdateStatus(int taskId, string newStatus)
        {
            var task = db.Tasks.Find(taskId);
            if (task == null) return HttpNotFound();

            // Get the current user role
            var userRole = Session["Role"]?.ToString();

            // Only allow Admins or Owners to update the status
            if (userRole == "Admin" || userRole == "Owner")
            {
                task.Status = newStatus;
                db.SaveChanges();

                int userId = Convert.ToInt32(Session["UserId"]);
                db.LogActivity(userId, $"Updated task #{taskId} status to {newStatus}");
                db.SaveChanges(); // Save the log entry

                TempData["SuccessMessage"] = "Task status updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "You do not have permission to update the task status.";
            }

            return RedirectToAction("Details", new { id = taskId });
        }

        // GET: Tasks/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            FarmTask task = db.Tasks.Find(id);
            if (task == null)
            {
                return HttpNotFound();
            }

            return View(task);
        }
        // GET: Tasks/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            FarmTask task = db.Tasks.Find(id);
            if (task == null)
            {
                return HttpNotFound();
            }

            return View(task);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TaskId,Title,Description,DueDate,Status,AssignedTo,AssignedDepartment,IsRecurring,RecurrenceType")] FarmTask task)
        {
            if (ModelState.IsValid)
            {
                db.Entry(task).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(task);
        }

        // GET: Tasks/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!User.IsInRole("Owner"))
            {
                return RedirectToAction("AdminDashboard", "Dashboard"); // Redirect if not authorized
            }
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            FarmTask task = db.Tasks.Find(id);
            if (task == null)
            {
                return HttpNotFound();
            }

            return View(task);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            FarmTask task = db.Tasks.Find(id);
            if (task == null) return HttpNotFound();

            db.Tasks.Remove(task);
            db.SaveChanges();

            // Log the task deletion
            int userId = Convert.ToInt32(Session["UserId"]);
            db.LogActivity(userId, $"Deleted task: {task.Title} (ID: {id})");
            db.SaveChanges();

            return RedirectToAction("Index");
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        // Generate Recurring Tasks (Run daily via scheduled job)
        public void GenerateRecurringTasks()
        {
            var today = DateTime.UtcNow.Date;
            var recurringTasks = db.Tasks.Where(t => t.IsRecurring).ToList();

            foreach (var task in recurringTasks)
            {
                // Check if the task is due for regeneration based on last generated date and recurrence type
                if (task.LastGeneratedDate == null || ShouldGenerateTask(task.LastGeneratedDate.Value, task.RecurrenceType, today))
                {
                    var newTask = new FarmTask
                    {
                        Title = task.Title,
                        Description = task.Description,
                        DueDate = today,
                        Status = "Pending",
                        AssignedTo = task.AssignedTo,
                        AssignedDepartment = task.AssignedDepartment,
                        IsRecurring = true,
                        RecurrenceType = task.RecurrenceType,
                        LastGeneratedDate = today
                    };

                    db.Tasks.Add(newTask);
                    task.LastGeneratedDate = today;
                }
            }

            db.SaveChanges();
        }

        // Helper to determine if a new recurring task should be created based on recurrence type
        private bool ShouldGenerateTask(DateTime lastDate, string recurrenceType, DateTime today)
        {
            today = today.ToUniversalTime(); // Ensure today's date is UTC
            switch (recurrenceType)
            {

                case "Daily":
                    return lastDate.AddDays(1) <= today;
                case "Weekly":
                    return lastDate.AddDays(7) <= today;
                case "Monthly":
                    return lastDate.AddMonths(1) <= today;
                default:
                    return false;
            }
        }


        // Helper: Get Current User ID from Session
        private int GetCurrentUserId()
        {
            if (Session["UserId"] != null)
            {
                return (int)Session["UserId"];
            }

            // Handle case if session is not available, e.g., redirect to login page
            return 0;
        }


        // Helper: Get Current User's Department
        private string GetCurrentUserDepartment()
        {
            var userId = GetCurrentUserId();
            return db.Users.Where(u => u.UserId == userId).Select(u => u.Department).FirstOrDefault();
        }

        // Helper: Get Current User Role
        private string GetCurrentUserRole()
        {
            var userId = GetCurrentUserId();
            var userRole = db.Users.Where(u => u.UserId == userId).Select(u => u.Role).FirstOrDefault();

            return userRole ?? "User"; // Returns the user's role if found, otherwise "User" as a fallback
        }

        // Helper: Get Current User Role
        /*private string GetCurrentUserRole()
        {
            var userId = GetCurrentUserId();
            return db.Users.Where(u => u.UserId == userId).Select(u => u.Role).FirstOrDefault();
        }*/

        // Helper: Get Departments List
        private List<SelectListItem> GetDepartmentList()
        {
            return db.Users
                .Where(u => !string.IsNullOrEmpty(u.Department))
                .Select(u => u.Department)
                .Distinct()
                .Select(d => new SelectListItem { Value = d, Text = d })
                .ToList();
        }

        // Helper: Notify User (Email, SMS, In-App)
        private void NotifyUser(int userId, string message)
        {
            var user = db.Users.Find(userId);
            if (user != null)
            {
                // Replace with actual email/SMS notification logic
                Console.WriteLine($"Sending notification to {user.Email}: {message}");
            }
        }

        // GET: TaskStatus/Create?taskId=5
        /*public ActionResult Status(int taskId)
        {
            var task = db.Tasks.Find(taskId);
            if (task == null) return HttpNotFound();

            var updates = db.TaskUpdates
                .Where(u => u.TaskId == taskId)
                .OrderByDescending(u => u.DateUpdated)
                .ToList();

            var viewModel = new TaskStatusViewModel
            {
                NewUpdate = new TaskUpdate { TaskId = taskId },
                PreviousUpdates = updates,
                TaskTitle = task.Title
            };

            return View(viewModel);
        }*/
        public ActionResult Status(int taskId)
        {
            var task = db.Tasks.Find(taskId);
            if (task == null)
                return HttpNotFound();

            var update = new TaskUpdate
            {
                TaskId = taskId
            };

            ViewBag.TaskTitle = task.Title;
            ViewBag.Progress = task.Progress;

            return View(update);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Status(TaskUpdate update)
        {
            var task = db.Tasks.Find(update.TaskId);
            if (!ModelState.IsValid)
            {
                //var task = db.Tasks.Find(update.TaskId);
                ViewBag.TaskTitle = task?.Title ?? "Unknown Task";
                ViewBag.Progress = task?.Progress ?? 0;
                return View(update);
            }

            var userIdObj = Session["UserId"];
            if (userIdObj == null)
                return RedirectToAction("Login", "Account");

            update.UpdatedBy = Convert.ToInt32(userIdObj);
            update.DateUpdated = DateTime.Now;
            
            if (task != null)
            {
                switch (update.TasksStatus)
                {
                    case "Started": task.Progress = 10; break;
                    case "On Hold": task.Progress = 30; break;
                    case "In Progress": task.Progress = 50; break;
                    case "Completed": task.Progress = 100; break;
                }
                db.Entry(task).State = EntityState.Modified;
            }

            db.TaskUpdates.Add(update);
            db.SaveChanges();

            // 🔔 Notify Admins & Owners
            var admins = db.Users.Where(u => u.Role == "Admin" || u.Role == "Owner").ToList();
            var taskInfo = db.Tasks.Find(update.TaskId);

            var conversation = db.Messages
                .FirstOrDefault(m => m.Subject.Contains("Task #" + update.TaskId));

            int conversationId = conversation?.ConversationId ?? conversation?.MessageId ?? 0;

            foreach (var admin in admins)
            {
                var message = new Message
                {
                    SenderId = update.UpdatedBy,
                    RecipientId = admin.UserId,
                    Subject = $"Task #{update.TaskId}: {taskInfo.Title} Update",
                    Body = $"A new status update was submitted for Task ID {update.TaskId}.<br/><br/>" +
                           (!string.IsNullOrWhiteSpace(update.Comments)
                               ? $"<strong>Comment:</strong> {update.Comments}<br/><br/>"
                               : "") +
                           $"<a href='/Task/Details/{update.TaskId}'>View Task</a>",
                    SentAt = DateTime.Now,
                    IsRead = false,
                    ConversationId = conversationId > 0 ? conversationId : (int?)null
                };

                db.Messages.Add(message);
                db.SaveChanges();

                if (conversationId == 0)
                {
                    message.ConversationId = message.MessageId;
                    conversationId = message.MessageId;
                    db.Entry(message).Property(m => m.ConversationId).IsModified = true;
                    db.SaveChanges();
                }
            }

            TempData["Message"] = "Task status updated.";
            return RedirectToAction("Status", new { taskId = update.TaskId });
        }



        // Optional: Timeline view
        public ActionResult Timeline(int taskId)
        {
            var updates = db.TaskUpdates
                .Where(ts => ts.TaskId == taskId)
                .OrderByDescending(ts => ts.DateUpdated)
                .ToList();

            ViewBag.Task = db.Tasks.Find(taskId);
            return View(updates);
        }
        public ActionResult MyTask()
        {
            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Find(userId);

            if (user == null)
                return HttpNotFound();

            var userTasks = db.Tasks
                .Where(t => t.AssignedTo == userId || t.AssignedDepartment == user.Department)
                .OrderByDescending(t => t.DueDate)
                .ToList();

            return View(userTasks);
        }


        public ActionResult ViewStatusUpdates(int? taskId, int? userId, string taskStatus, string search)
        {
            var updates = db.TaskUpdates
                .Include(u => u.User)
                .Include(u => u.Task)
                .AsQueryable();

            // Mark all unseen as seen
            var unseenUpdates = updates.Where(ts => !ts.SeenByAdmin).ToList();
            foreach (var update in unseenUpdates)
            {
                update.SeenByAdmin = true;
            }

            if (taskId.HasValue)
                updates = updates.Where(u => u.TaskId == taskId.Value);

            if (userId.HasValue)
                updates = updates.Where(u => u.UpdatedBy == userId.Value);

            if (!string.IsNullOrEmpty(taskStatus))
                updates = updates.Where(u => u.TasksStatus == taskStatus);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                updates = updates.Where(u =>
                    u.Task.Title.ToLower().Contains(search) ||
                    u.User.FullName.ToLower().Contains(search)
                );
            }

            ViewBag.Users = new SelectList(db.Users, "UserId", "FullName");
            ViewBag.Tasks = new SelectList(db.Tasks, "TaskId", "Title");
            ViewBag.SelectedTaskId = taskId;
            ViewBag.SelectedUserId = userId;
            ViewBag.SelectedStatus = taskStatus;
            ViewBag.Search = search;

            return View(updates.OrderByDescending(u => u.DateUpdated).ToList());
        }




    }
}