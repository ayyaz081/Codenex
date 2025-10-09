using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CodeNex.Models;

namespace CodeNex.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // DbSet properties
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Solution> Solutions => Set<Solution>();
        public DbSet<Publication> Publications => Set<Publication>();
        public DbSet<Repository> Repositories => Set<Repository>();
        public new DbSet<User> Users => Set<User>();
        public DbSet<PublicationComment> PublicationComments => Set<PublicationComment>();
        public DbSet<PublicationRating> PublicationRatings => Set<PublicationRating>();
        public DbSet<ContactForm> ContactForms => Set<ContactForm>();
        public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<UserPurchase> UserPurchases => Set<UserPurchase>();

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

            // Configure Payment relationships
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete conflicts

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Repository)
                .WithMany()
                .HasForeignKey(p => p.RepositoryId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete conflicts

            // Configure UserPurchase relationships
            modelBuilder.Entity<UserPurchase>()
                .HasOne(up => up.User)
                .WithMany()
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete conflicts

            modelBuilder.Entity<UserPurchase>()
                .HasOne(up => up.Repository)
                .WithMany()
                .HasForeignKey(up => up.RepositoryId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete conflicts

            modelBuilder.Entity<UserPurchase>()
                .HasOne(up => up.Payment)
                .WithMany()
                .HasForeignKey(up => up.PaymentId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete conflicts
        }
    }
}
