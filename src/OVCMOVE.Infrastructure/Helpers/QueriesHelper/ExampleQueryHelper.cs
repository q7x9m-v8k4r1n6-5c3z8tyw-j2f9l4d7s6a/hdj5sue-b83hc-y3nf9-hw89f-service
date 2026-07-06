namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class ExampleQueryHelper
{
    public static string GetAllExampleByFilterQuery()
    {
        return @"
            SELECT
                Id,
                Name
            FROM ExampleTable
            WHERE 1 = 1";
    }
}
