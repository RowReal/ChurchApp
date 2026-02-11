using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Worker> Workers { get; set; }
        public DbSet<Directorate> Directorates { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<AuditTrail> AuditTrails { get; set; }
        public DbSet<ProfileUpdateRequest> ProfileUpdateRequests { get; set; }
        public DbSet<RejectionNotification> RejectionNotifications { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ExcuseRequest> ExcuseRequests { get; set; }
        public DbSet<ExcuseRequestHistory> ExcuseRequestHistories { get; set; }
        public DbSet<BreakRequest> BreakRequests { get; set; }
        public DbSet<BreakRequestHistory> BreakRequestHistories { get; set; }
        public DbSet<OfferingType> OfferingTypes { get; set; }
        public DbSet<OfferingRecord> OfferingRecords { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AccountabilityCase> AccountabilityCases { get; set; }
        public DbSet<AccountabilityMessage> AccountabilityMessages { get; set; }
        public DbSet<CaseAction> CaseActions { get; set; }
        public DbSet<WorkerHierarchy> WorkerHierarchies { get; set; }

        public DbSet<RecordNomination> RecordNominations { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<ServiceNote> ServiceNotes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Worker configurations
            modelBuilder.Entity<Worker>()
                .HasIndex(w => w.WorkerId)
                .IsUnique();

            modelBuilder.Entity<Worker>()
                .HasOne(w => w.Supervisor)
                .WithMany(w => w.Subordinates)
                .HasForeignKey(w => w.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Directorate configurations
            modelBuilder.Entity<Directorate>()
                .HasIndex(d => d.Code)
                .IsUnique();

            modelBuilder.Entity<Directorate>()
                .HasIndex(d => d.Name)
                .IsUnique();

            // Directorate Head relationships
            modelBuilder.Entity<Directorate>()
                .HasOne(d => d.HeadWorker)
                .WithMany()
                .HasForeignKey(d => d.HeadWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Directorate>()
                .HasOne(d => d.AssistantHeadWorker)
                .WithMany()
                .HasForeignKey(d => d.AssistantHeadWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Directorate - Department relationship
            modelBuilder.Entity<Directorate>()
                .HasMany(d => d.Departments)
                .WithOne(d => d.Directorate)
                .HasForeignKey(d => d.DirectorateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Department configurations
            modelBuilder.Entity<Department>()
                .HasIndex(d => new { d.Name, d.DirectorateId })
                .IsUnique();

            // Department Head relationships
            modelBuilder.Entity<Department>()
                .HasOne(d => d.HeadWorker)
                .WithMany()
                .HasForeignKey(d => d.HeadWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Department>()
                .HasOne(d => d.AssistantHeadWorker)
                .WithMany()
                .HasForeignKey(d => d.AssistantHeadWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Department - Unit relationship
            modelBuilder.Entity<Department>()
                .HasMany(d => d.Units)
                .WithOne(u => u.Department)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unit configurations
            modelBuilder.Entity<Unit>()
                .HasIndex(u => new { u.Name, u.DepartmentId })
                .IsUnique();

            // Unit - Worker relationship
            modelBuilder.Entity<Unit>()
                .HasOne(u => u.LeaderWorker)
                .WithMany()
                .HasForeignKey(u => u.LeaderWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Audit Trail configurations
            modelBuilder.Entity<AuditTrail>()
                .HasIndex(a => new { a.TableName, a.RecordId });

            modelBuilder.Entity<AuditTrail>()
                .HasIndex(a => a.ChangedAt);

            modelBuilder.Entity<AuditTrail>()
                .HasOne(a => a.ChangedByWorker)
                .WithMany()
                .HasForeignKey(a => a.ChangedByWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Profile Update Request configurations
            modelBuilder.Entity<ProfileUpdateRequest>()
                .HasOne(p => p.Worker)
                .WithMany()
                .HasForeignKey(p => p.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProfileUpdateRequest>()
                .HasOne(p => p.ApprovedByWorker)
                .WithMany()
                .HasForeignKey(p => p.ApprovedByWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProfileUpdateRequest>()
                .HasOne(p => p.ApproverWorker)
                .WithMany()
                .HasForeignKey(p => p.ApproverWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Rejection Notification configurations
            modelBuilder.Entity<RejectionNotification>()
                .HasOne(r => r.ProfileUpdateRequest)
                .WithMany()
                .HasForeignKey(r => r.ProfileUpdateRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RejectionNotification>()
                .HasOne(r => r.Worker)
                .WithMany()
                .HasForeignKey(r => r.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RejectionNotification>()
                .HasOne(r => r.RejectedByWorker)
                .WithMany()
                .HasForeignKey(r => r.RejectedByWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Service configurations
            modelBuilder.Entity<Service>()
                .HasIndex(s => s.Name)
                .IsUnique();

            // Excuse Request configurations
            modelBuilder.Entity<ExcuseRequest>()
                .HasOne(e => e.Worker)
                .WithMany()
                .HasForeignKey(e => e.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExcuseRequest>()
                .HasOne(e => e.Service)
                .WithMany(s => s.ExcuseRequests)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExcuseRequest>()
                .HasOne(e => e.NominatedBackup)
                .WithMany()
                .HasForeignKey(e => e.NominatedBackupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExcuseRequest>()
                .HasOne(e => e.Supervisor)
                .WithMany()
                .HasForeignKey(e => e.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExcuseRequest>()
                .HasOne(e => e.ApprovedByWorker)
                .WithMany()
                .HasForeignKey(e => e.ApprovedByWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExcuseRequestHistory>()
                .HasOne(h => h.ExcuseRequest)
                .WithMany(e => e.History)
                .HasForeignKey(h => h.ExcuseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExcuseRequestHistory>()
                .HasOne(h => h.ActionByWorker)
                .WithMany()
                .HasForeignKey(h => h.ActionByWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Break Request configurations
            modelBuilder.Entity<BreakRequest>()
                .HasOne(br => br.Worker)
                .WithMany()
                .HasForeignKey(br => br.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BreakRequest>()
                .HasOne(br => br.RelieveOfficer)
                .WithMany()
                .HasForeignKey(br => br.RelieveOfficerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BreakRequest>()
                .HasOne(br => br.Supervisor)
                .WithMany()
                .HasForeignKey(br => br.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BreakRequest>()
                .HasOne(br => br.ApprovedByWorker)
                .WithMany()
                .HasForeignKey(br => br.ApprovedByWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Break Request History configurations
            modelBuilder.Entity<BreakRequestHistory>()
                .HasOne(brh => brh.BreakRequest)
                .WithMany(br => br.History)
                .HasForeignKey(brh => brh.BreakRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BreakRequestHistory>()
                .HasOne(brh => brh.ActionByWorker)
                .WithMany()
                .HasForeignKey(brh => brh.ActionByWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // In OnModelCreating method:
            modelBuilder.Entity<ServiceNote>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(sn => sn.Service)
                    .WithMany()
                    .HasForeignKey(sn => sn.ServiceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sn => sn.RecordedBy)
                    .WithMany()
                    .HasForeignKey(sn => sn.RecordedByWorkerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(sn => sn.ThemeOrSermonTitle).HasMaxLength(200);
                entity.Property(sn => sn.MinisterOrGuestSpeaker).HasMaxLength(100);
                entity.Property(sn => sn.TechnicalIssues).HasMaxLength(1000);
                entity.Property(sn => sn.OrderOfServiceChanges).HasMaxLength(1000);
                entity.Property(sn => sn.Disruptions).HasMaxLength(1000);
                entity.Property(sn => sn.SafetyConcerns).HasMaxLength(1000);
                entity.Property(sn => sn.NotableGuests).HasMaxLength(500);
                entity.Property(sn => sn.AttendancePattern).HasMaxLength(500);
                entity.Property(sn => sn.SpecialParticipation).HasMaxLength(500);
                entity.Property(sn => sn.LeadershipAwareness).HasMaxLength(2000);
                entity.Property(sn => sn.FollowUpNeeded).HasMaxLength(1000);
                entity.Property(sn => sn.AdditionalNotes).HasMaxLength(4000);
            });

            // In your AppDbContext.cs, update OfferingRecord configuration:
            modelBuilder.Entity<OfferingRecord>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.OfferingTypeName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PaymentMode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
                entity.Property(e => e.Remarks).HasMaxLength(500);
                entity.Property(e => e.RecordedByName).HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ApprovedByName).HasMaxLength(100);
                entity.Property(e => e.AdminComments).HasMaxLength(500);
                entity.Property(e => e.RecordedDate).HasDefaultValueSql("GETUTCDATE()");

                // Relationships - Using WorkerId
                entity.HasOne(e => e.Service)
                      .WithMany()
                      .HasForeignKey(e => e.ServiceId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.OfferingType)
                      .WithMany(o => o.OfferingRecords)
                      .HasForeignKey(e => e.OfferingTypeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.RecordedBy)
                      .WithMany()
                      .HasForeignKey(e => e.RecordedByWorkerId)
                      .HasPrincipalKey(w => w.WorkerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ApprovedBy)
                      .WithMany()
                      .HasForeignKey(e => e.ApprovedByWorkerId)
                      .HasPrincipalKey(w => w.WorkerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // OfferingType configuration
            modelBuilder.Entity<OfferingType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CreatorName).HasMaxLength(100);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");

                // Relationship with Worker (Creator) using WorkerId
                entity.HasOne(e => e.Creator)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedBy)
                      .HasPrincipalKey(w => w.WorkerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // NEW: AttendanceRecord configuration
            // In your OnModelCreating method, find the AttendanceRecord configuration:
            // NEW: AttendanceRecord configuration
            modelBuilder.Entity<AttendanceRecord>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.AttendanceDate)
                      .IsRequired()
                      .HasColumnType("date");

                // Add ServiceId
                entity.Property(e => e.ServiceId)
                      .IsRequired();

                entity.Property(e => e.ServiceName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Men)
                      .IsRequired()
                      .HasDefaultValue(0);

                entity.Property(e => e.Women)
                      .IsRequired()
                      .HasDefaultValue(0);

                entity.Property(e => e.Teenagers)
                      .IsRequired()
                      .HasDefaultValue(0);

                entity.Property(e => e.Children)
                      .IsRequired()
                      .HasDefaultValue(0);

                entity.Property(e => e.Notes)
                      .HasMaxLength(500);

                // CHANGED: Use RecordedByWorkerId (string) instead of RecordedById (int)
                entity.Property(e => e.RecordedByWorkerId)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(e => e.RecordedDate)
                      .IsRequired()
                      .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.ModifiedDate)
                      .HasDefaultValueSql("datetime('now')");

                // Relationships
                // CHANGED: Use WorkerId as foreign key
                entity.HasOne(e => e.RecordedBy)
                      .WithMany()
                      .HasForeignKey(e => e.RecordedByWorkerId)
                      .HasPrincipalKey(w => w.WorkerId)  // Important: Use WorkerId as principal key
                      .OnDelete(DeleteBehavior.Restrict);

                // Add Service relationship
                entity.HasOne(e => e.Service)
                      .WithMany()
                      .HasForeignKey(e => e.ServiceId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Indexes for better query performance
                entity.HasIndex(e => e.AttendanceDate);
                entity.HasIndex(e => e.ServiceName);
                entity.HasIndex(e => e.RecordedByWorkerId);
                entity.HasIndex(e => e.ServiceId);
            });

            // Add this in the OnModelCreating method of AppDbContext.cs
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.UpdatedBy)
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("datetime('now')");

                // Unique constraint on Name
                entity.HasIndex(r => r.Name)
                    .IsUnique();
            });

            // Configure AccountabilityCase relationships
            modelBuilder.Entity<AccountabilityCase>()
                .HasOne(c => c.Worker)
                .WithMany()
                .HasForeignKey(c => c.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AccountabilityCase>()
                .HasOne(c => c.CreatedByWorker)
                .WithMany()
                .HasForeignKey(c => c.CreatedByWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AccountabilityCase>()
                .HasOne(c => c.AssignedToWorker)
                .WithMany()
                .HasForeignKey(c => c.AssignedToWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure AccountabilityMessage relationships
            modelBuilder.Entity<AccountabilityMessage>()
                .HasOne(m => m.Case)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AccountabilityMessage>()
                .HasOne(m => m.SenderWorker)
                .WithMany()
                .HasForeignKey(m => m.SenderWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure WorkerHierarchy relationships
            modelBuilder.Entity<WorkerHierarchy>()
                .HasOne(h => h.Worker)
                .WithMany()
                .HasForeignKey(h => h.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkerHierarchy>()
                .HasOne(h => h.ReportsToWorker)
                .WithMany()
                .HasForeignKey(h => h.ReportsToWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkerHierarchy>()
                .HasOne(h => h.Directorate)
                .WithMany()
                .HasForeignKey(h => h.DirectorateId)
                .OnDelete(DeleteBehavior.Restrict);
            // Configure RecordNomination
            modelBuilder.Entity<RecordNomination>()
                .HasOne(rn => rn.Service)
                .WithMany()
                .HasForeignKey(rn => rn.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RecordNomination>()
       .HasOne(rn => rn.Service)
       .WithMany()
       .HasForeignKey(rn => rn.ServiceId)
       .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RecordNomination>()
         .HasIndex(rn => new { rn.ServiceId, rn.ServiceDate, rn.NomineeWorkerId, rn.RecordType })
         .IsUnique();

            // Add index for faster queries
            modelBuilder.Entity<RecordNomination>()
                .HasIndex(rn => new { rn.ServiceId, rn.ServiceDate, rn.NomineeWorkerId, rn.RecordType })
                .IsUnique();

            // Guest configurations
            modelBuilder.Entity<Guest>(entity =>
            {
                entity.HasKey(g => g.Id);

                // Guest number is unique
                entity.HasIndex(g => g.GuestNumber)
                      .IsUnique();

                // Indexes for faster queries
                entity.HasIndex(g => g.RecordingDate);
                entity.HasIndex(g => g.VisitingDate);
                entity.HasIndex(g => new { g.IsSecondTimer, g.IsActive });
                entity.HasIndex(g => g.RecordedByWorkerId);
                entity.HasIndex(g => g.ServiceId);
                entity.HasIndex(g => g.PhoneNumber)
                      .HasFilter("[PhoneNumber] IS NOT NULL AND [PhoneNumber] != ''");
                entity.HasIndex(g => g.Email)
                      .HasFilter("[Email] IS NOT NULL AND [Email] != ''");

                // Properties
                entity.Property(g => g.GuestNumber)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(g => g.Title)
                      .HasMaxLength(20);

                entity.Property(g => g.FirstName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(g => g.MiddleName)
                      .HasMaxLength(100);

                entity.Property(g => g.Surname)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(g => g.Sex)
                      .IsRequired()
                      .HasMaxLength(10);

                entity.Property(g => g.AgeGroup)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(g => g.PhoneNumber)
                      .HasMaxLength(20);

                entity.Property(g => g.WhatsAppNumber)
                      .HasMaxLength(20);

                entity.Property(g => g.Email)
                      .HasMaxLength(100);

                entity.Property(g => g.RecordedByWorkerId)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(g => g.CreatedDate)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Relationships
                entity.HasOne(g => g.Service)
                      .WithMany()
                      .HasForeignKey(g => g.ServiceId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Foreign key to Worker using WorkerId
                entity.HasOne<Worker>()
                      .WithMany()
                      .HasForeignKey(g => g.RecordedByWorkerId)
                      .HasPrincipalKey(w => w.WorkerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });


        }

    }
}