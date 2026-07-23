using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace OVCMOVE.Application.Abstractions
{
    public interface IUnitOfWork : IDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction? Transaction { get; }

        void Begin();
        void Commit();
        void Rollback();
    }
}
