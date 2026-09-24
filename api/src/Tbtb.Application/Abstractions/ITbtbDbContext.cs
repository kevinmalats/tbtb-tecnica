using Microsoft.EntityFrameworkCore;
using Tbtb.Domain;

namespace Tbtb.Application.Abstractions;

public interface ITbtbDbContext
{
    DbSet<Country> Countries { get; }
    DbSet<City> Cities { get; }
    DbSet<DocumentType> DocumentTypes { get; }
    DbSet<Actor> Actors { get; }
    DbSet<Patient> Patients { get; }
    DbSet<FollowUp> FollowUps { get; }
    DbSet<Contact> Contacts { get; }
    DbSet<ContactRevision> ContactRevisions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
