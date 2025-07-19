using EventManagementSystem.Core;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Core entity tables
        public DbSet<User> Users { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventCategory> EventCategories { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<IssuedTicket> IssuedTickets { get; set; }
        public DbSet<EmailOutbox> EmailOutbox { get; set; }
        public DbSet<EventAssistant> EventAssistants { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Configure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(e => e.Email)
                .IsUnique();

            modelBuilder.Entity<Event>()
                .HasIndex(e => e.UrlSlug)
                .IsUnique();

            modelBuilder.Entity<IssuedTicket>()
                .HasIndex(t => t.UniqueReferenceCode)
                .IsUnique();

            // ✅ Configure decimal precision
            modelBuilder.Entity<TicketType>()
                .Property(t => t.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Venue>(entity =>
            {
                entity.Property(v => v.Latitude).HasColumnType("decimal(10,8)");
                entity.Property(v => v.Longitude).HasColumnType("decimal(11,8)");
            });

            // ✅ ULTIMATE FIX: Use RESTRICT for ALL foreign keys to break cascade cycles

            // User relationships - ALL RESTRICT
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Registration>()
                .HasOne(r => r.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Event relationships - ALL RESTRICT
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(e => e.VenueID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CategoryID)
                .OnDelete(DeleteBehavior.SetNull); // This is safe since it's nullable

            modelBuilder.Entity<Registration>()
                .HasOne(r => r.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(r => r.EventID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketType>()
                .HasOne(t => t.Event)
                .WithMany(e => e.TicketTypes)
                .HasForeignKey(t => t.EventID)
                .OnDelete(DeleteBehavior.Restrict);

            // Ticket relationships - ALL RESTRICT
            modelBuilder.Entity<IssuedTicket>()
                .HasOne(t => t.Registration)
                .WithMany(r => r.IssuedTickets)
                .HasForeignKey(t => t.RegisterID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IssuedTicket>()
                .HasOne(t => t.TicketType)
                .WithMany(tt => tt.IssuedTickets)
                .HasForeignKey(t => t.TicketTypeID)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ CRITICAL FIX: CheckedInByUser with NO ACTION (not even SetNull)
            modelBuilder.Entity<IssuedTicket>()
                .HasOne(t => t.CheckedInByUser)
                .WithMany()
                .HasForeignKey(t => t.CheckedInByUserID)
                .OnDelete(DeleteBehavior.NoAction); // ✅ This should fix the cascade conflict

            // ✅ Prevent duplicate registrations for same user/event
            modelBuilder.Entity<Registration>()
                .HasIndex(r => new { r.UserID, r.EventID })
                .IsUnique();

            // ✅ FIXED: EmailOutbox configuration
            modelBuilder.Entity<EmailOutbox>(entity =>
            {
                entity.HasKey(e => e.EmailID); // Using int primary key

                entity.Property(e => e.Body)
                    .HasMaxLength(int.MaxValue); // Use nvarchar(max) for large content

                entity.Property(e => e.AttachmentsJson)
                    .HasMaxLength(int.MaxValue);

                // Indexes for performance
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.Status, e.NextRetryAt });
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.RelatedEntityId);
            }); // ✅ FIXED: Proper closing here

            // ✅ FIXED: EventAssistant configuration (now INSIDE the method)
            modelBuilder.Entity<EventAssistant>()
                .HasOne(ea => ea.Event)
                .WithMany()
                .HasForeignKey(ea => ea.EventID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventAssistant>()
                .HasOne(ea => ea.Assistant)
                .WithMany()
                .HasForeignKey(ea => ea.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EventAssistant>()
                .HasOne(ea => ea.AssignedBy)
                .WithMany()
                .HasForeignKey(ea => ea.AssignedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate assignments
            modelBuilder.Entity<EventAssistant>()
                .HasIndex(ea => new { ea.EventID, ea.UserID })
                .IsUnique();
        }
    }
}