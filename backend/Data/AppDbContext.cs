using Microsoft.EntityFrameworkCore;
using PollSystem.Models.Entities;

namespace PollSystem.Data
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserFollow> UserFollows => Set<UserFollow>();
        public DbSet<UserCosmetic> UserCosmetics => Set<UserCosmetic>();
        public DbSet<Poll> Polls => Set<Poll>();
        public DbSet<PollOption> PollOptions => Set<PollOption>();
        public DbSet<PollTag> PollTags => Set<PollTag>();
        public DbSet<Vote> Votes => Set<Vote>();
        public DbSet<VoteSelection> VoteSelections => Set<VoteSelection>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(user => user.Id);
                entity.Property(user => user.Username)
                    .HasMaxLength(40)
                    .UseCollation("NOCASE")
                    .IsRequired();
                entity.HasIndex(user => user.Username).IsUnique();
                entity.Property(user => user.PasswordHash).IsRequired();
            });

            modelBuilder.Entity<UserFollow>(entity =>
            {
                entity.HasKey(follow => new { follow.FollowerId, follow.FollowedId });
                entity.HasOne(follow => follow.Follower)
                    .WithMany(user => user.Following)
                    .HasForeignKey(follow => follow.FollowerId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(follow => follow.Followed)
                    .WithMany(user => user.Followers)
                    .HasForeignKey(follow => follow.FollowedId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserCosmetic>(entity =>
            {
                entity.HasKey(cosmetic => cosmetic.Id);
                entity.Property(cosmetic => cosmetic.Name).HasMaxLength(80).IsRequired();
                entity.HasIndex(cosmetic => new { cosmetic.UserId, cosmetic.Name }).IsUnique();
                entity.HasOne<User>()
                    .WithMany(user => user.Cosmetics)
                    .HasForeignKey(cosmetic => cosmetic.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Poll>(entity =>
            {
                entity.HasKey(poll => poll.Id);
                entity.Property(poll => poll.Question).HasMaxLength(200).IsRequired();
                entity.Property(poll => poll.Category).HasMaxLength(50).IsRequired();
                entity.HasMany(poll => poll.Options)
                    .WithOne()
                    .HasForeignKey(option => option.PollId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(poll => poll.Tags)
                    .WithOne()
                    .HasForeignKey(tag => tag.PollId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(poll => poll.Votes)
                    .WithOne()
                    .HasForeignKey(vote => vote.PollId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(poll => poll.Comments)
                    .WithOne()
                    .HasForeignKey(comment => comment.PollId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PollOption>(entity =>
            {
                entity.HasKey(option => option.Id);
                entity.Property(option => option.text).HasMaxLength(200).IsRequired();
                entity.HasIndex(option => new { option.PollId, option.Position }).IsUnique();
            });

            modelBuilder.Entity<PollTag>(entity =>
            {
                entity.HasKey(tag => tag.Id);
                entity.Property(tag => tag.Value).HasMaxLength(80).IsRequired();
            });

            modelBuilder.Entity<Vote>(entity =>
            {
                entity.HasKey(vote => vote.Id);
                entity.HasIndex(vote => new { vote.PollId, vote.UserId });
                entity.HasMany(vote => vote.Selections)
                    .WithOne()
                    .HasForeignKey(selection => selection.VoteId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(vote => vote.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VoteSelection>(entity =>
            {
                entity.HasKey(selection => selection.Id);
                entity.HasIndex(selection => new { selection.VoteId, selection.Position }).IsUnique();
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(comment => comment.Id);
                entity.Property(comment => comment.AuthorName).HasMaxLength(40).IsRequired();
                entity.Property(comment => comment.Content).HasMaxLength(500).IsRequired();
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(comment => comment.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(notification => notification.Id);
                entity.Property(notification => notification.Content).HasMaxLength(1000).IsRequired();
                entity.HasIndex(notification => new { notification.TargetUserId, notification.CreatedAt });
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(notification => notification.TargetUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
