using OVCMOVE.Application.Abstractions;
using OVCMOVE.Infrastructure.Persistance.SqlServer;
using System;
using System.Data;

namespace OVCMOVE.Infrastructure.Common
{
    public class UnitOfWork : IUnitOfWork
    {
        private bool _disposed;

        public UnitOfWork(ISqlServerFactory sqlServerFactory)
        {
            Connection = sqlServerFactory.CreateConnection();
        }

        public IDbConnection Connection { get; }
        public IDbTransaction? Transaction { get; private set; }

        public void Begin()
        {
            if (Transaction is not null)
            {
                return;
            }

            EnsureConnectionOpen();
            Transaction = Connection.BeginTransaction();
        }

        public void Commit()
        {
            if (Transaction is null)
            {
                throw new InvalidOperationException("No active transaction to commit.");
            }

            Transaction.Commit();
            Transaction.Dispose();
            Transaction = null;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Transaction?.Dispose();

            if (Connection.State != ConnectionState.Closed)
            {
                Connection.Close();
            }

            Connection.Dispose();
            _disposed = true;
        }

        public void Rollback()
        {
            if (Transaction is null)
            {
                return;
            }

            Transaction.Rollback();
            Transaction.Dispose();
            Transaction = null;
        }

        private void EnsureConnectionOpen()
        {
            if (Connection.State == ConnectionState.Open)
            {
                return;
            }

            Connection.Open();
        }
    }
}
