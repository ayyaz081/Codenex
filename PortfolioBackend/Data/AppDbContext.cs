using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortfolioBackend.Models;

namespace PortfolioBackend.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // DbSet properties
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Solution> Solutions => Set<Solution>();
        public DbSet<Domain> Domains => Set<Domain>();
        public DbSet<Publication> Publications => Set<Publication>();
        public DbSet<Repository> Repositories => Set<Repository>();
        public new DbSet<User> Users => Set<User>();
        public DbSet<PublicationComment> PublicationComments => Set<PublicationComment>();
        public DbSet<PublicationRating> PublicationRatings => Set<PublicationRating>();
        public DbSet<ContactForm> ContactForms => Set<ContactForm>();
        public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
        // Removed About, Home, Team, Testimonial-related models

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<PublicationComment>()
                .HasOne(pc => pc.Publication)
                .WithMany(p => p.Comments)
                .HasForeignKey(pc => pc.PublicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PublicationComment>()
                .HasOne(pc => pc.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(pc => pc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PublicationRating>()
                .HasOne(pr => pr.Publication)
                .WithMany(p => p.Ratings)
                .HasForeignKey(pr => pr.PublicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PublicationRating>()
                .HasOne(pr => pr.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContactForm>()
                .HasOne(cf => cf.User)
                .WithMany(u => u.ContactForms)
                .HasForeignKey(cf => cf.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ensure unique email for users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Ensure unique rating per user per publication
            modelBuilder.Entity<PublicationRating>()
                .HasIndex(pr => new { pr.UserId, pr.PublicationId })
                .IsUnique();

            // Configure CommentLike relationships
            modelBuilder.Entity<CommentLike>()
                .HasOne(cl => cl.Comment)
                .WithMany(c => c.Likes)
                .HasForeignKey(cl => cl.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommentLike>()
                .HasOne(cl => cl.User)
                .WithMany(u => u.CommentLikes)
                .HasForeignKey(cl => cl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ensure unique like per user per comment
            modelBuilder.Entity<CommentLike>()
                .HasIndex(cl => new { cl.UserId, cl.CommentId })
                .IsUnique();

            // Removed display order indexes for TeamMember, ClientTestimonial, and CarouselSlide

        }
    }
}
