using FarmTrack.Models;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Web.Mvc;

namespace FarmTrack.Controllers
{
    public class NotificationController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        [HttpGet]
        public JsonResult Check()
        {
            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            int unreadDirect = db.Messages
                .Count(m => m.RecipientId == userId && !m.IsRead);

            int pendingTasks = db.Tasks
                .Count(t => t.AssignedTo == userId && t.Status != "Completed");

            int lowStockItems = db.Inventories
                .Count(i => i.Quantity <= i.LowStockThreshold);

            int newStatusUpdates = 0;
            if (user.Role == "Admin" || user.Role == "Owner")
            {
                newStatusUpdates = db.TaskUpdates.Count(ts => !ts.SeenByAdmin);
            }


            return Json(new
            {
                unreadMessages = unreadDirect,
                pendingTask = pendingTasks,
                lowStock = lowStockItems,
                newStatusUpdate = newStatusUpdates
            }, JsonRequestBehavior.AllowGet);
        }

        public PartialViewResult Dropdown()
        {
            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);
            var notifications = new List<NotificationViewModel>();

            // Fetch raw data and then project
            var unreadMessages = db.Messages
                .Where(m => m.RecipientId == userId && !m.IsRead)
                .Select(m => new {
                    m.SentAt,
                    SenderName = m.Sender.FullName
                })
                .ToList()
                .Select(m => new NotificationViewModel
                {
                    Type = "Message",
                    Content = $"New message from {m.SenderName}",
                    Time = m.SentAt
                });

            var tasks = db.Tasks
                .Where(t => t.AssignedTo == userId && t.Status != "Completed")
                .Select(t => new {
                    t.Title,
                    t.DueDate
                })
                .ToList()
                .Select(t => new NotificationViewModel
                {
                    Type = "Task",
                    Content = $"Pending task: {t.Title}",
                    Time = t.DueDate
                });

            var lowStock = db.Inventories
                .Where(i => i.Quantity <= i.LowStockThreshold)
                .Select(i => new {
                    i.ItemName,
                    i.Quantity
                })
                .ToList()
                .Select(i => new NotificationViewModel
                {
                    Type = "Inventory",
                    Content = $"Low stock: {i.ItemName} ({i.Quantity} left)",
                    Time = DateTime.Now
                });

            var statusUpdates = new List<NotificationViewModel>();
            if (user.Role == "Admin" || user.Role == "Owner")
            {
                statusUpdates = db.TaskUpdates
                    .Where(tu => !tu.SeenByAdmin)
                    .Select(tu => new {
                        tu.DateUpdated,
                        UserName = tu.User.FullName
                    })
                    .ToList()
                    .Select(tu => new NotificationViewModel
                    {
                        Type = "StatusUpdate",
                        Content = $"Task update from {tu.UserName}",
                        Time = tu.DateUpdated
                    })
                    .ToList();
            }

            notifications.AddRange(unreadMessages);
            notifications.AddRange(tasks);
            notifications.AddRange(lowStock);
            notifications.AddRange(statusUpdates);

            var sorted = notifications.OrderByDescending(n => n.Time).Take(10).ToList();
            return PartialView("_NotificationDropdown", sorted);
        }


    }
}
