using System.Data;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Infrastructure.Persistance.SqlServer;

namespace OVCMOVE.Infrastructure.Common;

public class UnitOfWork : IUnitOfWork
{
    public IDbConnection Connection { get; }
    public IDbTransaction? Transaction { get; private set; }

    public UnitOfWork(ISqlServerFactory sqlServerFactory)
    {
        Connection = sqlServerFactory.CreateConnection();
    }

    public void Begin()
    {
        if (Connection.State != ConnectionState.Open)
        {
            Connection.Open();
        }

        Transaction = Connection.BeginTransaction();
    }

    public void Commit()
    {
        Transaction?.Commit();
        Transaction?.Dispose();
        Transaction = null;
    }

    public void Rollback()
    {
        Transaction?.Rollback();
        Transaction?.Dispose();
        Transaction = null;
    }

    public void Dispose()
    {
        Transaction?.Dispose();
        Connection.Dispose();
    }
}
