using OVCMOVE.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace OVCMOVE.Infrastructure.Common
{
    public class UnitOfWork : IUnitOfWork
    {
        public IDbConnection Connection { get; }
        public IDbTransaction Transaction { get; private set; }

        public void Begin()
        {
            Transaction = Connection.BeginTransaction();
        }

        public void Commit()
        {
            Transaction.Commit();
        }

        public void Dispose()
        {
            Transaction?.Dispose();
            Connection?.Dispose();
        }

        public void Rollback()
        {
            Transaction.Rollback();
        }
    }
}
