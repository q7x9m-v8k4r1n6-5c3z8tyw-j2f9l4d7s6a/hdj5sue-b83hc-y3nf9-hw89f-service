namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class ExampleQueryHelper
{
    public static string GetAllExampleByFilterQuery()
    {
        return @"
            SELECT
                Id,
                Name
            FROM ExampleTable WITH (NOLOCK)
            WHERE 1 = 1";
    }
}
