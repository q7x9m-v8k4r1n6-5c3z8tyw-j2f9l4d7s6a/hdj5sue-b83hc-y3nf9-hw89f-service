using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace OVCMOVE.Infrastructure.Persistence.SqlServer
{
    public interface ISqlServerFactory
    {
        IDbConnection CreateConnection();
    }
}
