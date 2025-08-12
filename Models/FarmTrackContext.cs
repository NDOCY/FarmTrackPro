using FarmPro.Models;
using FarmTrack.Models;
//using FarmTrackPro.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace FarmTrack.Models
{
    public class FarmTrackContext : DbContext
    {
        public FarmTrackContext() : base("FarmTrackDB")
        {
            this.Configuration.LazyLoadingEnabled = false;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Livestock> Livestocks { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<FarmTask> Tasks { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<HealthRecord> HealthRecords { get; set; }
        public DbSet<TaskUpdate> TaskUpdates { get; set; } // New DbSet for TaskStatus
        public DbSet<ReproductionRecord> ReproductionRecords { get; set; } // New DbSet for ReproductionRecord
        public DbSet<HealthCheckSchedule> HealthCheckSchedules { get; set; } // New DbSet for Notifications
        public DbSet<HealthCheckLivestock> HealthCheckLivestocks { get; set; } // New DbSet for Notifications
        public DbSet<Equipment> Equipments { get; set; } // New DbSet for Equipment
        public DbSet<EquipmentRepair> EquipmentRepairs { get; set; } // New DbSet for Equipment
        public DbSet<EquipmentRepairLog> EquipmentRepairLogs { get; set; } // New DbSet for Equipment
        public DbSet<WeightRecord> WeightRecords { get; set; } // New DbSet for WeightRecord
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<InventoryRestock> InventoryRestocks { get; set; }
        public DbSet<Veterinarian> Veterinarians { get; set; } // New DbSet for Veterinarian
        public DbSet<EmergencyContact> EmergencyContacts { get; set; } // New DbSet for EmergencyContact
        public DbSet<EmergencyGuideline> EmergencyGuidelines { get; set; } // New DbSet for EmergencyGuideline
        public DbSet<CareGuideline> CareGuidelines { get; set; } // New DbSet for CareGuideline
        public DbSet<Crop> Crops { get; set; }
        public DbSet<Plot> Plots { get; set; }
        public DbSet<CropRequirements> CropRequirement { get; set; }
        //public DbSet<CropAssignment> CropAssignments { get; set; }
        public DbSet<PlotCrop> PlotCrops { get; set; }
        public DbSet<PreparationTask> PreparationTasks { get; set; } // New DbSet for PreparationTask
        public DbSet<Activity> Activities { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<GrowthRecord> GrowthRecords { get; set; }
        public DbSet<HarvestOutcome> HarvestOutcomes { get; set; }





        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            // Livestock → User
            modelBuilder.Entity<Livestock>()
                .HasRequired(l => l.User)
                .WithMany(u => u.Livestocks)
                .HasForeignKey(l => l.UserId);

            // Inventory → User
            modelBuilder.Entity<Inventory>()
                .HasRequired(i => i.User)
                .WithMany(u => u.Inventories)
                .HasForeignKey(i => i.UserId);

            // Job → User
            modelBuilder.Entity<Job>()
                .HasRequired(j => j.User)
                .WithMany(u => u.Jobs)
                .HasForeignKey(j => j.UserId);

            // Task → AssignedUser
            modelBuilder.Entity<FarmTask>()
                .HasRequired(t => t.AssignedUser)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.AssignedTo);

            // JobApplication → User
            modelBuilder.Entity<JobApplication>()
                .HasRequired(ja => ja.User)
                .WithMany()
                .HasForeignKey(ja => ja.UserId)
                .WillCascadeOnDelete(false);

            // JobApplication → Job
            modelBuilder.Entity<JobApplication>()
                .HasRequired(ja => ja.Job)
                .WithMany()
                .HasForeignKey(ja => ja.JobId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<HealthRecord>()
                .HasRequired(ja => ja.Livestock)
                .WithMany()
                .HasForeignKey(ja => ja.LivestockId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<HealthCheckLivestock>()
                .HasRequired(ja => ja.Livestock)
                .WithMany()
                .HasForeignKey(ja => ja.LivestockId)
                .WillCascadeOnDelete(false);

            // Message → Sender
            modelBuilder.Entity<Message>()
                .HasRequired(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .WillCascadeOnDelete(false);

            // Message → Recipient (optional)
            modelBuilder.Entity<Message>()
                .HasOptional(m => m.Recipient)
                .WithMany()
                .HasForeignKey(m => m.RecipientId)
                .WillCascadeOnDelete(false);

            // Message → ReplyToMessage (self-reference)
            modelBuilder.Entity<Message>()
                .HasOptional(m => m.ReplyToMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ReplyToMessageId)
                .WillCascadeOnDelete(false);
            // HealthRecord → Livestock
            modelBuilder.Entity<Livestock>()
            .HasMany(l => l.Offspring)
            .WithOptional(o => o.Parent)
            .HasForeignKey(o => o.ParentId);

            modelBuilder.Entity<FarmTask>()
            .HasOptional(t => t.PlotCrop)
            .WithMany(pc => pc.Tasks)
            .HasForeignKey(t => t.PlotCropId);

        }

        public void LogActivity(int userId, string description)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Description = description,
                Timestamp = DateTime.Now
            };

            this.ActivityLogs.Add(log);
            this.SaveChanges();
        }
    }
}
