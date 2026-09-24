using Microsoft.EntityFrameworkCore;
using Tbtb.Application.Abstractions;
using Tbtb.Domain;

namespace Tbtb.Infrastructure.Persistence;

public sealed class TbtbDbContext(DbContextOptions<TbtbDbContext> options) : DbContext(options), ITbtbDbContext
{
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactRevision> ContactRevisions => Set<ContactRevision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(entity => { entity.ToTable("Country"); entity.HasKey(x => x.Code); entity.Property(x => x.Code).HasColumnType("char(2)"); entity.Property(x => x.Name).HasMaxLength(80); });
        modelBuilder.Entity<City>(entity => { entity.ToTable("City"); entity.HasKey(x => x.Id); entity.Property(x => x.CountryCode).HasColumnType("char(2)"); entity.Property(x => x.Name).HasMaxLength(120); entity.HasOne(x => x.Country).WithMany().HasForeignKey(x => x.CountryCode); });
        modelBuilder.Entity<DocumentType>(entity => { entity.ToTable("DocumentType"); entity.HasKey(x => x.Code); entity.Property(x => x.Code).HasMaxLength(20).IsUnicode(false); entity.Property(x => x.Name).HasMaxLength(80); });
        modelBuilder.Entity<Actor>(entity => { entity.ToTable("Actor"); entity.HasKey(x => x.Id); entity.Property(x => x.DisplayName).HasMaxLength(120); entity.Property(x => x.Role).HasMaxLength(20).IsUnicode(false); });
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patient"); entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(150); entity.Property(x => x.DocumentCountryCode).HasColumnType("char(2)");
            entity.Property(x => x.DocumentTypeCode).HasMaxLength(20).IsUnicode(false); entity.Property(x => x.DocumentNumber).HasMaxLength(40);
            entity.Property(x => x.NormalizedDocumentNumber).HasMaxLength(40); entity.Property(x => x.Phone).HasMaxLength(30); entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(3);
            entity.HasIndex(x => new { x.DocumentCountryCode, x.DocumentTypeCode, x.NormalizedDocumentNumber }).IsUnique();
            entity.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId);
            entity.HasOne(x => x.AssignedManager).WithMany().HasForeignKey(x => x.AssignedManagerId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<FollowUp>(entity => { entity.ToTable("FollowUp"); entity.HasKey(x => x.Id); entity.Property(x => x.ScheduledAtUtc).HasPrecision(3); entity.Property(x => x.CreatedAtUtc).HasPrecision(3); entity.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId); });
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("Contact"); entity.HasKey(x => x.Id); entity.Property(x => x.CreatedAtUtc).HasPrecision(3); entity.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            entity.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId); entity.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.CityAtContact).WithMany().HasForeignKey(x => x.CityAtContactId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<ContactRevision>(entity =>
        {
            entity.ToTable("ContactRevision"); entity.HasKey(x => new { x.ContactId, x.RevisionNumber });
            entity.Property(x => x.OccurredAtUtc).HasPrecision(3); entity.Property(x => x.RecordedAtUtc).HasPrecision(3); entity.Property(x => x.Channel).HasMaxLength(16).IsUnicode(false);
            entity.Property(x => x.Result).HasMaxLength(20).IsUnicode(false); entity.Property(x => x.CorrectionReason).HasMaxLength(500);
            entity.HasOne(x => x.Contact).WithMany(x => x.Revisions).HasForeignKey(x => x.ContactId);
            entity.HasOne(x => x.RecordedByActor).WithMany().HasForeignKey(x => x.RecordedBy).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
