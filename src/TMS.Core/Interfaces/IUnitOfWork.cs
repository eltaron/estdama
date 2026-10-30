using TMS.Core.Entities.Common;
using TMS.Core.Interfaces;

namespace TMS.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync();
}
