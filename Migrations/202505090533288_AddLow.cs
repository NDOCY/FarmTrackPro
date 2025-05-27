namespace FarmTrack.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLow : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ActivityLogs",
                c => new
                    {
                        LogId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        Description = c.String(),
                        Timestamp = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.LogId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        FullName = c.String(nullable: false),
                        Email = c.String(nullable: false),
                        PasswordHash = c.String(nullable: false),
                        PhoneNumber = c.String(),
                        Address = c.String(),
                        ProfilePictureUrl = c.String(),
                        DateRegistered = c.DateTime(nullable: false),
                        Department = c.String(),
                        Role = c.String(),
                        ID = c.String(),
                        CV = c.String(),
                    })
                .PrimaryKey(t => t.UserId);
            
            CreateTable(
                "dbo.Inventories",
                c => new
                    {
                        InventoryId = c.Int(nullable: false, identity: true),
                        ItemName = c.String(nullable: false),
                        Category = c.String(),
                        Quantity = c.Int(nullable: false),
                        DateAdded = c.DateTime(nullable: false),
                        UserId = c.Int(nullable: false),
                        Notes = c.String(),
                        LowStockThreshold = c.Int(nullable: false),
                        LastRestocked = c.DateTime(),
                    })
                .PrimaryKey(t => t.InventoryId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.JobApplications",
                c => new
                    {
                        ID = c.String(nullable: false, maxLength: 128),
                        JobApplicationId = c.Int(nullable: false),
                        JobId = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        AppliedDate = c.DateTime(nullable: false),
                        PhoneNumber = c.String(),
                        Address = c.String(),
                        CV = c.String(),
                        Status = c.String(),
                        ReviewNotes = c.String(),
                        Education = c.String(),
                        Institution = c.String(),
                        Experience = c.String(),
                        InterviewDate = c.DateTime(),
                        InterviewVenue = c.String(),
                        InterviewerName = c.String(),
                        Job_JobId = c.Int(),
                        User_UserId = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Jobs", t => t.Job_JobId)
                .ForeignKey("dbo.Jobs", t => t.JobId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .ForeignKey("dbo.Users", t => t.User_UserId)
                .Index(t => t.JobId)
                .Index(t => t.UserId)
                .Index(t => t.Job_JobId)
                .Index(t => t.User_UserId);
            
            CreateTable(
                "dbo.Jobs",
                c => new
                    {
                        JobId = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false),
                        Description = c.String(nullable: false),
                        JobType = c.String(),
                        DatePosted = c.DateTime(nullable: false),
                        UserId = c.Int(nullable: false),
                        Location = c.String(),
                        EmploymentType = c.String(),
                        ApplicationDeadline = c.DateTime(nullable: false),
                        SalaryRange = c.String(),
                        RequiredSkills = c.String(),
                    })
                .PrimaryKey(t => t.JobId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Livestocks",
                c => new
                    {
                        LivestockId = c.Int(nullable: false, identity: true),
                        Type = c.String(nullable: false),
                        Breed = c.String(nullable: false),
                        TagNumber = c.String(),
                        Notes = c.String(),
                        UserId = c.Int(nullable: false),
                        DateRegistered = c.DateTime(nullable: false),
                        ImagePath = c.String(),
                        QrCodePath = c.String(),
                        Status = c.String(),
                    })
                .PrimaryKey(t => t.LivestockId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.HealthRecords",
                c => new
                    {
                        HealthRecordId = c.Int(nullable: false, identity: true),
                        LivestockId = c.Int(nullable: false),
                        EventType = c.String(nullable: false),
                        Notes = c.String(),
                        Date = c.DateTime(nullable: false),
                        RecordedBy = c.String(),
                    })
                .PrimaryKey(t => t.HealthRecordId)
                .ForeignKey("dbo.Livestocks", t => t.LivestockId)
                .Index(t => t.LivestockId);
            
            CreateTable(
                "dbo.Messages",
                c => new
                    {
                        MessageId = c.Int(nullable: false, identity: true),
                        SenderId = c.Int(nullable: false),
                        RecipientId = c.Int(),
                        Department = c.String(),
                        IsToAdmins = c.Boolean(nullable: false),
                        IsGroupMessage = c.Boolean(nullable: false),
                        ConversationId = c.Int(),
                        ReplyToMessageId = c.Int(),
                        SentAt = c.DateTime(nullable: false),
                        Subject = c.String(),
                        Body = c.String(),
                        IsRead = c.Boolean(nullable: false),
                        User_UserId = c.Int(),
                        User_UserId1 = c.Int(),
                    })
                .PrimaryKey(t => t.MessageId)
                .ForeignKey("dbo.Users", t => t.RecipientId)
                .ForeignKey("dbo.Messages", t => t.ReplyToMessageId)
                .ForeignKey("dbo.Users", t => t.SenderId)
                .ForeignKey("dbo.Users", t => t.User_UserId)
                .ForeignKey("dbo.Users", t => t.User_UserId1)
                .Index(t => t.SenderId)
                .Index(t => t.RecipientId)
                .Index(t => t.ReplyToMessageId)
                .Index(t => t.User_UserId)
                .Index(t => t.User_UserId1);
            
            CreateTable(
                "dbo.FarmTasks",
                c => new
                    {
                        TaskId = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false),
                        Description = c.String(),
                        DueDate = c.DateTime(nullable: false),
                        AssignedTo = c.Int(nullable: false),
                        Status = c.String(nullable: false),
                        IsRecurring = c.Boolean(nullable: false),
                        AssignedDepartment = c.String(),
                        RecurrenceType = c.String(),
                        LastGeneratedDate = c.DateTime(),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.TaskId)
                .ForeignKey("dbo.Users", t => t.AssignedTo)
                .Index(t => t.AssignedTo);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ActivityLogs", "UserId", "dbo.Users");
            DropForeignKey("dbo.FarmTasks", "AssignedTo", "dbo.Users");
            DropForeignKey("dbo.Messages", "User_UserId1", "dbo.Users");
            DropForeignKey("dbo.Messages", "User_UserId", "dbo.Users");
            DropForeignKey("dbo.Messages", "SenderId", "dbo.Users");
            DropForeignKey("dbo.Messages", "ReplyToMessageId", "dbo.Messages");
            DropForeignKey("dbo.Messages", "RecipientId", "dbo.Users");
            DropForeignKey("dbo.Livestocks", "UserId", "dbo.Users");
            DropForeignKey("dbo.HealthRecords", "LivestockId", "dbo.Livestocks");
            DropForeignKey("dbo.JobApplications", "User_UserId", "dbo.Users");
            DropForeignKey("dbo.JobApplications", "UserId", "dbo.Users");
            DropForeignKey("dbo.JobApplications", "JobId", "dbo.Jobs");
            DropForeignKey("dbo.Jobs", "UserId", "dbo.Users");
            DropForeignKey("dbo.JobApplications", "Job_JobId", "dbo.Jobs");
            DropForeignKey("dbo.Inventories", "UserId", "dbo.Users");
            DropIndex("dbo.FarmTasks", new[] { "AssignedTo" });
            DropIndex("dbo.Messages", new[] { "User_UserId1" });
            DropIndex("dbo.Messages", new[] { "User_UserId" });
            DropIndex("dbo.Messages", new[] { "ReplyToMessageId" });
            DropIndex("dbo.Messages", new[] { "RecipientId" });
            DropIndex("dbo.Messages", new[] { "SenderId" });
            DropIndex("dbo.HealthRecords", new[] { "LivestockId" });
            DropIndex("dbo.Livestocks", new[] { "UserId" });
            DropIndex("dbo.Jobs", new[] { "UserId" });
            DropIndex("dbo.JobApplications", new[] { "User_UserId" });
            DropIndex("dbo.JobApplications", new[] { "Job_JobId" });
            DropIndex("dbo.JobApplications", new[] { "UserId" });
            DropIndex("dbo.JobApplications", new[] { "JobId" });
            DropIndex("dbo.Inventories", new[] { "UserId" });
            DropIndex("dbo.ActivityLogs", new[] { "UserId" });
            DropTable("dbo.FarmTasks");
            DropTable("dbo.Messages");
            DropTable("dbo.HealthRecords");
            DropTable("dbo.Livestocks");
            DropTable("dbo.Jobs");
            DropTable("dbo.JobApplications");
            DropTable("dbo.Inventories");
            DropTable("dbo.Users");
            DropTable("dbo.ActivityLogs");
        }
    }
}
