namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class UserQueryHelper
{
    public static string GetByUsernameQuery()
    {
        return @"
            SELECT * 
            FROM Users 
            WHERE Username = @Username 
              AND Status = @Status";
    }

    public static string GetByEmailQuery()
    {
        return @"
            SELECT * 
            FROM Users 
            WHERE Email = @Email 
              AND Status = @Status";
    }

    public static string GetByIdQuery()
    {
        return @"
            SELECT * 
            FROM Users 
            WHERE Id = @Id 
              AND Status = @Status";
    }
}