using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace OVCMOVE.Infrastructure.Persistance.SqlServer
{
    public interface ISqlServerFactory
    {
        IDbConnection CreateConnection();
    }
}
