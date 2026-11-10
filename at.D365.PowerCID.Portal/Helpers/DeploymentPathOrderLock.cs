using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data;
using System.Threading.Tasks;

namespace at.D365.PowerCID.Portal.Helpers
{
    public static class DeploymentPathOrderLock
    {
        public static async Task<bool> AcquireAsync(DbContext dbContext, string resource)
        {
            var transaction = dbContext.Database.CurrentTransaction ??
                throw new InvalidOperationException("An active database transaction is required to lock a deployment path order.");

            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = @lockTimeout; SELECT @result;";

            var resourceParameter = command.CreateParameter();
            resourceParameter.ParameterName = "@resource";
            resourceParameter.DbType = DbType.String;
            resourceParameter.Size = 255;
            resourceParameter.Value = resource;
            command.Parameters.Add(resourceParameter);

            var timeoutParameter = command.CreateParameter();
            timeoutParameter.ParameterName = "@lockTimeout";
            timeoutParameter.DbType = DbType.Int32;
            timeoutParameter.Value = 10000;
            command.Parameters.Add(timeoutParameter);

            var result = await command.ExecuteScalarAsync();
            if (result is not int lockResult)
                throw new InvalidOperationException("SQL Server did not return a deployment path order lock result.");

            return lockResult >= 0;
        }
    }
}
