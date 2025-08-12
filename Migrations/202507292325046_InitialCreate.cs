namespace FarmPro.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
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
                        Barcode = c.String(),
                        QrCodePath = c.String(),
                        Notes = c.String(),
                        NotifySupplier = c.Boolean(nullable: false),
                        RestockThreshold = c.Int(nullable: false),
                        LowStockThreshold = c.Int(nullable: false),
                        LastRestocked = c.DateTime(),
                        SupplierId = c.Int(),
                    })
                .PrimaryKey(t => t.InventoryId)
                .ForeignKey("dbo.Suppliers", t => t.SupplierId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.SupplierId);
            
            CreateTable(
                "dbo.Suppliers",
                c => new
                    {
                        SupplierId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        ContactPerson = c.String(),
                        PhoneNumber = c.String(),
                        Email = c.String(),
                        Address = c.String(),
                    })
                .PrimaryKey(t => t.SupplierId);
            
            CreateTable(
                "dbo.InventoryRestocks",
                c => new
                    {
                        InventoryRestockId = c.Int(nullable: false, identity: true),
                        InventoryId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        RequestedOn = c.DateTime(nullable: false),
                        SupplierNotified = c.Boolean(nullable: false),
                        IsCompleted = c.Boolean(nullable: false),
                        Failed = c.Boolean(nullable: false),
                        CompletedOn = c.DateTime(),
                        RequestedById = c.Int(nullable: false),
                        SupplierId = c.Int(nullable: false),
                        RequestedBy_UserId = c.Int(),
                    })
                .PrimaryKey(t => t.InventoryRestockId)
                .ForeignKey("dbo.Inventories", t => t.InventoryId)
                .ForeignKey("dbo.Users", t => t.RequestedBy_UserId)
                .ForeignKey("dbo.Suppliers", t => t.SupplierId)
                .Index(t => t.InventoryId)
                .Index(t => t.SupplierId)
                .Index(t => t.RequestedBy_UserId);
            
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
                        InterviewEmailSent = c.Boolean(nullable: false),
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
                        Sex = c.String(),
                        Status = c.String(),
                        Age = c.Double(nullable: false),
                        Weight = c.Double(nullable: false),
                        InitialWeight = c.Double(),
                        DateOfBirth = c.DateTime(),
                        IsBreedingStock = c.Boolean(nullable: false),
                        ParentId = c.Int(),
                        ReproductionRecordId = c.Int(),
                        ReproductionRecord_Id = c.Int(),
                    })
                .PrimaryKey(t => t.LivestockId)
                .ForeignKey("dbo.ReproductionRecords", t => t.ReproductionRecord_Id)
                .ForeignKey("dbo.Livestocks", t => t.ParentId)
                .ForeignKey("dbo.ReproductionRecords", t => t.ReproductionRecordId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.ParentId)
                .Index(t => t.ReproductionRecordId)
                .Index(t => t.ReproductionRecord_Id);
            
            CreateTable(
                "dbo.ReproductionRecords",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FemaleLivestockId = c.Int(nullable: false),
                        MaleLivestockId = c.Int(),
                        BreedingDate = c.DateTime(nullable: false),
                        ExpectedDueDate = c.DateTime(),
                        ActualBirthDate = c.DateTime(),
                        BirthOutcome = c.String(),
                        Notes = c.String(),
                        NumberOfOffspring = c.Int(),
                        IsBirthRecorded = c.Boolean(nullable: false),
                        FemaleLivestock_LivestockId = c.Int(),
                        MaleLivestock_LivestockId = c.Int(),
                        Livestock_LivestockId = c.Int(),
                        Livestock_LivestockId1 = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Livestocks", t => t.FemaleLivestock_LivestockId)
                .ForeignKey("dbo.Livestocks", t => t.MaleLivestock_LivestockId)
                .ForeignKey("dbo.Livestocks", t => t.Livestock_LivestockId)
                .ForeignKey("dbo.Livestocks", t => t.Livestock_LivestockId1)
                .Index(t => t.FemaleLivestock_LivestockId)
                .Index(t => t.MaleLivestock_LivestockId)
                .Index(t => t.Livestock_LivestockId)
                .Index(t => t.Livestock_LivestockId1);
            
            CreateTable(
                "dbo.HealthRecords",
                c => new
                    {
                        HealthRecordId = c.Int(nullable: false, identity: true),
                        LivestockId = c.Int(nullable: false),
                        EventType = c.String(nullable: false),
                        Notes = c.String(),
                        Weight = c.Double(),
                        Diagnosis = c.String(),
                        Treatment = c.String(),
                        Date = c.DateTime(nullable: false),
                        RecordedBy = c.String(),
                        Livestock_LivestockId = c.Int(),
                    })
                .PrimaryKey(t => t.HealthRecordId)
                .ForeignKey("dbo.Livestocks", t => t.LivestockId)
                .ForeignKey("dbo.Livestocks", t => t.Livestock_LivestockId)
                .Index(t => t.LivestockId)
                .Index(t => t.Livestock_LivestockId);
            
            CreateTable(
                "dbo.WeightRecords",
                c => new
                    {
                        WeightRecordId = c.Int(nullable: false, identity: true),
                        LivestockId = c.Int(nullable: false),
                        Weight = c.Double(nullable: false),
                        RecordedAt = c.DateTime(nullable: false),
                        Notes = c.String(),
                    })
                .PrimaryKey(t => t.WeightRecordId)
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
                        DependsOnTaskId = c.Int(),
                        Progress = c.Int(nullable: false),
                        Comments = c.String(),
                        PlotCrop_Id = c.Int(),
                    })
                .PrimaryKey(t => t.TaskId)
                .ForeignKey("dbo.Users", t => t.AssignedTo)
                .ForeignKey("dbo.FarmTasks", t => t.DependsOnTaskId)
                .ForeignKey("dbo.PlotCrops", t => t.PlotCrop_Id)
                .Index(t => t.AssignedTo)
                .Index(t => t.DependsOnTaskId)
                .Index(t => t.PlotCrop_Id);
            
            CreateTable(
                "dbo.CareGuidelines",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AnimalType = c.String(nullable: false, maxLength: 50),
                        IssueType = c.String(nullable: false, maxLength: 100),
                        EmergencyLevel = c.String(nullable: false),
                        ImmediateActions = c.String(nullable: false),
                        WhatNotToDo = c.String(),
                        WhenToCallVet = c.String(),
                        AdditionalNotes = c.String(),
                        CreatedDate = c.DateTime(nullable: false),
                        LastUpdated = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CropRequirements",
                c => new
                    {
                        CropId = c.Int(nullable: false),
                        ScientificName = c.String(maxLength: 100),
                        Type = c.String(maxLength: 30),
                        PlantingSeason = c.String(maxLength: 30),
                        GrowthDurationDays = c.Int(),
                        ExpectedYieldKgPerHectare = c.Double(),
                        PreferredSoil = c.String(maxLength: 100),
                        GrowthDuration = c.String(),
                        MinTemperature = c.String(),
                        CommonPestsDiseases = c.String(maxLength: 100),
                        Notes = c.String(maxLength: 1000),
                        Source = c.String(maxLength: 100),
                        CropRequirement_CropId = c.Int(),
                    })
                .PrimaryKey(t => t.CropId)
                .ForeignKey("dbo.Crops", t => t.CropId)
                .ForeignKey("dbo.CropRequirements", t => t.CropRequirement_CropId)
                .Index(t => t.CropId)
                .Index(t => t.CropRequirement_CropId);
            
            CreateTable(
                "dbo.Crops",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        Variety = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Plots",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        CropId = c.Int(),
                        SoilType = c.String(maxLength: 30),
                        IrrigationMethod = c.String(maxLength: 30),
                        FertilizerType = c.String(maxLength: 50),
                        SizeInHectares = c.Double(nullable: false),
                        PlantingDate = c.DateTime(),
                        MaturityDate = c.DateTime(),
                        Statas = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        Coordinates = c.String(maxLength: 500),
                        LastInspectionDate = c.DateTime(),
                        IrrigationFrequency = c.Int(),
                        Notes = c.String(maxLength: 1000),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Crops", t => t.CropId)
                .Index(t => t.CropId);
            
            CreateTable(
                "dbo.EmergencyContacts",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ContactType = c.String(nullable: false, maxLength: 50),
                        Name = c.String(nullable: false, maxLength: 100),
                        Phone = c.String(nullable: false, maxLength: 20),
                        Email = c.String(maxLength: 100),
                        AvailableHours = c.String(maxLength: 50),
                        IsPrimary = c.Boolean(nullable: false),
                        Notes = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.EmergencyGuidelines",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AnimalType = c.String(),
                        Condition = c.String(),
                        FirstAidSteps = c.String(),
                        Notes = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.EquipmentRepairLogs",
                c => new
                    {
                        EquipmentRepairLogId = c.Int(nullable: false, identity: true),
                        EquipmentId = c.Int(nullable: false),
                        RepairDate = c.DateTime(nullable: false),
                        IssueReported = c.String(),
                        RepairDetails = c.String(),
                        RepairedBy = c.String(),
                        EquipmentRepair_RepairId = c.Int(),
                    })
                .PrimaryKey(t => t.EquipmentRepairLogId)
                .ForeignKey("dbo.EquipmentRepairs", t => t.EquipmentRepair_RepairId)
                .ForeignKey("dbo.Equipments", t => t.EquipmentId)
                .Index(t => t.EquipmentId)
                .Index(t => t.EquipmentRepair_RepairId);
            
            CreateTable(
                "dbo.Equipments",
                c => new
                    {
                        EquipmentId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        Description = c.String(),
                        SerialNumber = c.String(),
                        ImagePath = c.String(),
                        Category = c.String(),
                        Date = c.DateTime(),
                        Status = c.String(),
                    })
                .PrimaryKey(t => t.EquipmentId);
            
            CreateTable(
                "dbo.EquipmentRepairs",
                c => new
                    {
                        RepairId = c.Int(nullable: false, identity: true),
                        EquipmentId = c.Int(nullable: false),
                        RepairDate = c.DateTime(nullable: false),
                        Description = c.String(),
                        TechnicianType = c.String(),
                        InHouseUserId = c.Int(),
                        OutsourcedTechnicianName = c.String(),
                        OutsourcedEmail = c.String(),
                        Status = c.String(),
                        Cost = c.Decimal(nullable: false, precision: 18, scale: 2),
                        InHouseUser_UserId = c.Int(),
                    })
                .PrimaryKey(t => t.RepairId)
                .ForeignKey("dbo.Equipments", t => t.EquipmentId)
                .ForeignKey("dbo.Users", t => t.InHouseUser_UserId)
                .Index(t => t.EquipmentId)
                .Index(t => t.InHouseUser_UserId);
            
            CreateTable(
                "dbo.HealthCheckLivestocks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        HealthCheckScheduleId = c.Int(nullable: false),
                        LivestockId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.HealthCheckSchedules", t => t.HealthCheckScheduleId)
                .ForeignKey("dbo.Livestocks", t => t.LivestockId)
                .Index(t => t.HealthCheckScheduleId)
                .Index(t => t.LivestockId);
            
            CreateTable(
                "dbo.HealthCheckSchedules",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CheckType = c.String(nullable: false),
                        Notes = c.String(),
                        ScheduledDate = c.DateTime(nullable: false),
                        Status = c.String(),
                        IsOutsourced = c.Boolean(nullable: false),
                        VeterinarianId = c.Int(),
                        Purpose = c.String(maxLength: 200),
                        EstimatedCost = c.Decimal(precision: 18, scale: 2),
                        ActualCost = c.Decimal(precision: 18, scale: 2),
                        VetInstructions = c.String(),
                        RequiresFollowUp = c.Boolean(nullable: false),
                        FollowUpDate = c.DateTime(),
                        AssignedToUserId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.AssignedToUserId)
                .ForeignKey("dbo.Veterinarians", t => t.VeterinarianId)
                .Index(t => t.VeterinarianId)
                .Index(t => t.AssignedToUserId);
            
            CreateTable(
                "dbo.Veterinarians",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FullName = c.String(nullable: false, maxLength: 100),
                        Phone = c.String(maxLength: 20),
                        Email = c.String(maxLength: 100),
                        Specialization = c.String(maxLength: 100),
                        ClinicName = c.String(maxLength: 150),
                        Address = c.String(),
                        IsActive = c.Boolean(nullable: false),
                        Notes = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.PlotCrops",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PlotId = c.Int(nullable: false),
                        CropId = c.Int(nullable: false),
                        DateAssigned = c.DateTime(nullable: false),
                        DatePlanted = c.DateTime(),
                        ExpectedMaturityDate = c.DateTime(),
                        HarvestDate = c.DateTime(),
                        ExpectedYield = c.Double(nullable: false),
                        ActualYield = c.Double(),
                        Status = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Crops", t => t.CropId)
                .ForeignKey("dbo.Plots", t => t.PlotId)
                .Index(t => t.PlotId)
                .Index(t => t.CropId);
            
            CreateTable(
                "dbo.PreparationTasks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CropAssignmentId = c.Int(nullable: false),
                        TaskType = c.String(nullable: false),
                        Notes = c.String(),
                        IsCompleted = c.Boolean(nullable: false),
                        CompletedOn = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.TaskUpdates",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TaskId = c.Int(nullable: false),
                        TasksStatus = c.String(nullable: false),
                        Progress = c.Int(nullable: false),
                        Comments = c.String(),
                        SeenByAdmin = c.Boolean(nullable: false),
                        DateUpdated = c.DateTime(nullable: false),
                        UpdatedBy = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.FarmTasks", t => t.TaskId)
                .ForeignKey("dbo.Users", t => t.UpdatedBy)
                .Index(t => t.TaskId)
                .Index(t => t.UpdatedBy);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TaskUpdates", "UpdatedBy", "dbo.Users");
            DropForeignKey("dbo.TaskUpdates", "TaskId", "dbo.FarmTasks");
            DropForeignKey("dbo.FarmTasks", "PlotCrop_Id", "dbo.PlotCrops");
            DropForeignKey("dbo.PlotCrops", "PlotId", "dbo.Plots");
            DropForeignKey("dbo.PlotCrops", "CropId", "dbo.Crops");
            DropForeignKey("dbo.HealthCheckLivestocks", "LivestockId", "dbo.Livestocks");
            DropForeignKey("dbo.HealthCheckSchedules", "VeterinarianId", "dbo.Veterinarians");
            DropForeignKey("dbo.HealthCheckLivestocks", "HealthCheckScheduleId", "dbo.HealthCheckSchedules");
            DropForeignKey("dbo.HealthCheckSchedules", "AssignedToUserId", "dbo.Users");
            DropForeignKey("dbo.EquipmentRepairLogs", "EquipmentId", "dbo.Equipments");
            DropForeignKey("dbo.EquipmentRepairLogs", "EquipmentRepair_RepairId", "dbo.EquipmentRepairs");
            DropForeignKey("dbo.EquipmentRepairs", "InHouseUser_UserId", "dbo.Users");
            DropForeignKey("dbo.EquipmentRepairs", "EquipmentId", "dbo.Equipments");
            DropForeignKey("dbo.CropRequirements", "CropRequirement_CropId", "dbo.CropRequirements");
            DropForeignKey("dbo.CropRequirements", "CropId", "dbo.Crops");
            DropForeignKey("dbo.Plots", "CropId", "dbo.Crops");
            DropForeignKey("dbo.ActivityLogs", "UserId", "dbo.Users");
            DropForeignKey("dbo.FarmTasks", "DependsOnTaskId", "dbo.FarmTasks");
            DropForeignKey("dbo.FarmTasks", "AssignedTo", "dbo.Users");
            DropForeignKey("dbo.Messages", "User_UserId1", "dbo.Users");
            DropForeignKey("dbo.Messages", "User_UserId", "dbo.Users");
            DropForeignKey("dbo.Messages", "SenderId", "dbo.Users");
            DropForeignKey("dbo.Messages", "ReplyToMessageId", "dbo.Messages");
            DropForeignKey("dbo.Messages", "RecipientId", "dbo.Users");
            DropForeignKey("dbo.WeightRecords", "LivestockId", "dbo.Livestocks");
            DropForeignKey("dbo.Livestocks", "UserId", "dbo.Users");
            DropForeignKey("dbo.Livestocks", "ReproductionRecordId", "dbo.ReproductionRecords");
            DropForeignKey("dbo.Livestocks", "ParentId", "dbo.Livestocks");
            DropForeignKey("dbo.ReproductionRecords", "Livestock_LivestockId1", "dbo.Livestocks");
            DropForeignKey("dbo.HealthRecords", "Livestock_LivestockId", "dbo.Livestocks");
            DropForeignKey("dbo.HealthRecords", "LivestockId", "dbo.Livestocks");
            DropForeignKey("dbo.ReproductionRecords", "Livestock_LivestockId", "dbo.Livestocks");
            DropForeignKey("dbo.Livestocks", "ReproductionRecord_Id", "dbo.ReproductionRecords");
            DropForeignKey("dbo.ReproductionRecords", "MaleLivestock_LivestockId", "dbo.Livestocks");
            DropForeignKey("dbo.ReproductionRecords", "FemaleLivestock_LivestockId", "dbo.Livestocks");
            DropForeignKey("dbo.JobApplications", "User_UserId", "dbo.Users");
            DropForeignKey("dbo.JobApplications", "UserId", "dbo.Users");
            DropForeignKey("dbo.JobApplications", "JobId", "dbo.Jobs");
            DropForeignKey("dbo.Jobs", "UserId", "dbo.Users");
            DropForeignKey("dbo.JobApplications", "Job_JobId", "dbo.Jobs");
            DropForeignKey("dbo.Inventories", "UserId", "dbo.Users");
            DropForeignKey("dbo.Inventories", "SupplierId", "dbo.Suppliers");
            DropForeignKey("dbo.InventoryRestocks", "SupplierId", "dbo.Suppliers");
            DropForeignKey("dbo.InventoryRestocks", "RequestedBy_UserId", "dbo.Users");
            DropForeignKey("dbo.InventoryRestocks", "InventoryId", "dbo.Inventories");
            DropIndex("dbo.TaskUpdates", new[] { "UpdatedBy" });
            DropIndex("dbo.TaskUpdates", new[] { "TaskId" });
            DropIndex("dbo.PlotCrops", new[] { "CropId" });
            DropIndex("dbo.PlotCrops", new[] { "PlotId" });
            DropIndex("dbo.HealthCheckSchedules", new[] { "AssignedToUserId" });
            DropIndex("dbo.HealthCheckSchedules", new[] { "VeterinarianId" });
            DropIndex("dbo.HealthCheckLivestocks", new[] { "LivestockId" });
            DropIndex("dbo.HealthCheckLivestocks", new[] { "HealthCheckScheduleId" });
            DropIndex("dbo.EquipmentRepairs", new[] { "InHouseUser_UserId" });
            DropIndex("dbo.EquipmentRepairs", new[] { "EquipmentId" });
            DropIndex("dbo.EquipmentRepairLogs", new[] { "EquipmentRepair_RepairId" });
            DropIndex("dbo.EquipmentRepairLogs", new[] { "EquipmentId" });
            DropIndex("dbo.Plots", new[] { "CropId" });
            DropIndex("dbo.CropRequirements", new[] { "CropRequirement_CropId" });
            DropIndex("dbo.CropRequirements", new[] { "CropId" });
            DropIndex("dbo.FarmTasks", new[] { "PlotCrop_Id" });
            DropIndex("dbo.FarmTasks", new[] { "DependsOnTaskId" });
            DropIndex("dbo.FarmTasks", new[] { "AssignedTo" });
            DropIndex("dbo.Messages", new[] { "User_UserId1" });
            DropIndex("dbo.Messages", new[] { "User_UserId" });
            DropIndex("dbo.Messages", new[] { "ReplyToMessageId" });
            DropIndex("dbo.Messages", new[] { "RecipientId" });
            DropIndex("dbo.Messages", new[] { "SenderId" });
            DropIndex("dbo.WeightRecords", new[] { "LivestockId" });
            DropIndex("dbo.HealthRecords", new[] { "Livestock_LivestockId" });
            DropIndex("dbo.HealthRecords", new[] { "LivestockId" });
            DropIndex("dbo.ReproductionRecords", new[] { "Livestock_LivestockId1" });
            DropIndex("dbo.ReproductionRecords", new[] { "Livestock_LivestockId" });
            DropIndex("dbo.ReproductionRecords", new[] { "MaleLivestock_LivestockId" });
            DropIndex("dbo.ReproductionRecords", new[] { "FemaleLivestock_LivestockId" });
            DropIndex("dbo.Livestocks", new[] { "ReproductionRecord_Id" });
            DropIndex("dbo.Livestocks", new[] { "ReproductionRecordId" });
            DropIndex("dbo.Livestocks", new[] { "ParentId" });
            DropIndex("dbo.Livestocks", new[] { "UserId" });
            DropIndex("dbo.Jobs", new[] { "UserId" });
            DropIndex("dbo.JobApplications", new[] { "User_UserId" });
            DropIndex("dbo.JobApplications", new[] { "Job_JobId" });
            DropIndex("dbo.JobApplications", new[] { "UserId" });
            DropIndex("dbo.JobApplications", new[] { "JobId" });
            DropIndex("dbo.InventoryRestocks", new[] { "RequestedBy_UserId" });
            DropIndex("dbo.InventoryRestocks", new[] { "SupplierId" });
            DropIndex("dbo.InventoryRestocks", new[] { "InventoryId" });
            DropIndex("dbo.Inventories", new[] { "SupplierId" });
            DropIndex("dbo.Inventories", new[] { "UserId" });
            DropIndex("dbo.ActivityLogs", new[] { "UserId" });
            DropTable("dbo.TaskUpdates");
            DropTable("dbo.PreparationTasks");
            DropTable("dbo.PlotCrops");
            DropTable("dbo.Veterinarians");
            DropTable("dbo.HealthCheckSchedules");
            DropTable("dbo.HealthCheckLivestocks");
            DropTable("dbo.EquipmentRepairs");
            DropTable("dbo.Equipments");
            DropTable("dbo.EquipmentRepairLogs");
            DropTable("dbo.EmergencyGuidelines");
            DropTable("dbo.EmergencyContacts");
            DropTable("dbo.Plots");
            DropTable("dbo.Crops");
            DropTable("dbo.CropRequirements");
            DropTable("dbo.CareGuidelines");
            DropTable("dbo.FarmTasks");
            DropTable("dbo.Messages");
            DropTable("dbo.WeightRecords");
            DropTable("dbo.HealthRecords");
            DropTable("dbo.ReproductionRecords");
            DropTable("dbo.Livestocks");
            DropTable("dbo.Jobs");
            DropTable("dbo.JobApplications");
            DropTable("dbo.InventoryRestocks");
            DropTable("dbo.Suppliers");
            DropTable("dbo.Inventories");
            DropTable("dbo.Users");
            DropTable("dbo.ActivityLogs");
        }
    }
}
