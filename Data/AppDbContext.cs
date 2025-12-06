using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Codenex.Models;

namespace Codenex.Data
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
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<ClientTestimonial> ClientTestimonials => Set<ClientTestimonial>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<PublicationComment>()
                .HasOne(pc => pc.Publication)
                .WithMany(p => p.Comments)
                .HasForeignKey(pc => pc.PublicationId)
                .OnDelete(DeleteBehavior.Cascade); // Delete comments when publication deleted

            modelBuilder.Entity<PublicationComment>()
                .HasOne(pc => pc.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(pc => pc.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Preserve comments when user deleted

            modelBuilder.Entity<PublicationRating>()
                .HasOne(pr => pr.Publication)
                .WithMany(p => p.Ratings)
                .HasForeignKey(pr => pr.PublicationId)
                .OnDelete(DeleteBehavior.Cascade); // Delete ratings when publication deleted

            modelBuilder.Entity<PublicationRating>()
                .HasOne(pr => pr.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Preserve ratings when user deleted

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
                .OnDelete(DeleteBehavior.Cascade); // Delete likes when comment deleted

            modelBuilder.Entity<CommentLike>()
                .HasOne(cl => cl.User)
                .WithMany(u => u.CommentLikes)
                .HasForeignKey(cl => cl.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Preserve likes when user deleted

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

            // Configure Product-Repository relationship (Product has optional Repository)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Repository)
                .WithMany()
                .HasForeignKey(p => p.RepositoryId)
                .OnDelete(DeleteBehavior.SetNull); // Preserve product when repository deleted
            
            // Configure Solution-Repository relationship (Solution has optional Repository)
            modelBuilder.Entity<Solution>()
                .HasOne(s => s.Repository)
                .WithMany()
                .HasForeignKey(s => s.RepositoryId)
                .OnDelete(DeleteBehavior.SetNull); // Preserve solution when repository deleted

            // Configure Solution-Publication relationship
            modelBuilder.Entity<Publication>()
                .HasOne(p => p.Solution)
                .WithMany(s => s.Publications)
                .HasForeignKey(p => p.SolutionId)
                .OnDelete(DeleteBehavior.SetNull); // Preserve publication when solution deleted
            
            // Configure Product-Publication relationship
            modelBuilder.Entity<Publication>()
                .HasOne(p => p.Product)
                .WithMany(p => p.Publications)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.SetNull); // Preserve publication when product deleted

            // Configure decimal precision
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Repository>()
                .Property(r => r.Price)
                .HasPrecision(18, 2);
        }
    }
}
