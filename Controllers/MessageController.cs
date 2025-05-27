using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using FarmTrack.ViewModels;

namespace FarmTrack.Controllers
{
    public class MessageController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        // GET: Message
        public ActionResult Index()
        {
            return RedirectToAction("Inbox");
        }

        public ActionResult Compose()
        {
            var currentUser = db.Users.Find((int)Session["UserId"]);

            ViewBag.Recipients = new SelectList(db.Users.Where(u => u.Role == "User"), "UserId", "FullName");
            ViewBag.Departments = db.Users.Select(u => u.Department).Distinct().ToList();
            ViewBag.IsAdminOrOwner = currentUser.Role == "Owner" || currentUser.Role == "Admin";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Send(Message message, string groupOption, string selectedDepartment)
        {
            int senderId = (int)Session["UserId"];
            var sender = db.Users.Find(senderId);

            message.SenderId = senderId;
            message.SentAt = DateTime.Now;

            if (sender.Role == "User")
            {
                // Send to Admins/Owners
                message.IsToAdmins = true;
                message.RecipientId = null;
                message.IsGroupMessage = true;
            }
            else
            {
                if (groupOption == "All")
                {
                    message.RecipientId = null;
                    message.IsGroupMessage = true;
                }
                else if (groupOption == "Department")
                {
                    message.RecipientId = null;
                    message.Department = selectedDepartment;
                    message.IsGroupMessage = true;
                }
                // Else: RecipientId is already set via dropdown
            }

            db.Messages.Add(message);
            db.SaveChanges();

            TempData["Message"] = "Message sent successfully!";
            return RedirectToAction("Inbox");
        }

        /*public ActionResult Inbox()
        {
            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            var messages = db.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Where(m =>
                    m.RecipientId == userId ||
                    m.SenderId == userId ||
                    (m.IsToAdmins && (user.Role == "Admin" || user.Role == "Owner")) ||
                    (m.IsGroupMessage && m.Department != null && m.Department == user.Department))
                .OrderByDescending(m => m.SentAt)
                .ToList();

            return View(messages);
        }*/
        public ActionResult Inbox()
        {
            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            var userMessages = db.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Where(m =>
                    m.RecipientId == userId ||
                    m.SenderId == userId ||
                    (m.IsToAdmins && (user.Role == "Admin" || user.Role == "Owner")) ||
                    (m.IsGroupMessage && m.Department != null && m.Department == user.Department))
                .ToList();

            // Group messages by conversation
            var groupedMessages = userMessages
                .GroupBy(m => m.ConversationId ?? m.MessageId)
                .Select(g => g.OrderByDescending(m => m.SentAt).First()) // Take the latest message in each group
                .OrderByDescending(m => m.SentAt)
                .ToList();

            return View(groupedMessages);
        }


        public ActionResult Reply(int messageId)
        {
            var original = db.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .FirstOrDefault(m => m.MessageId == messageId);

            if (original == null) return HttpNotFound();

            int conversationId = original.ConversationId ?? original.MessageId;

            var thread = db.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId || m.MessageId == conversationId)
                .OrderBy(m => m.SentAt)
                .ToList();

            var viewModel = new ReplyViewModel
            {
                OriginalMessage = original,
                Replies = thread,
                NewReply = new Message
                {
                    ConversationId = conversationId,
                    ReplyToMessageId = messageId,
                    RecipientId = original.SenderId, // default to replying to sender
                    Subject = "RE: " + original.Subject
                }
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reply(ReplyViewModel model)
        {
            int senderId = (int)Session["UserId"];
            var reply = model.NewReply;

            reply.SenderId = senderId;
            reply.SentAt = DateTime.Now;

            db.Messages.Add(reply);
            db.SaveChanges();

            return RedirectToAction("Reply", new { messageId = reply.ConversationId });
        }


        public ActionResult Details(int id)
        {
            var message = db.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Include(m => m.Replies.Select(r => r.Sender))
                .FirstOrDefault(m => m.MessageId == id);

            if (message == null)
                return HttpNotFound();

            if (!message.IsRead && message.RecipientId == (int)Session["UserId"])
            {
                message.IsRead = true;
                db.SaveChanges();
            }

            return View(message);
        }
    }
}
