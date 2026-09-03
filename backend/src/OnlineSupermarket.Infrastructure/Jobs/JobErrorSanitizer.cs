namespace OnlineSupermarket.Infrastructure.Jobs;

public static class JobErrorSanitizer
{
    public static string Sanitize(Exception exception, int maxLength = 1000)
    {
        if (exception == null) return string.Empty;
        
        var message = exception.ToString();
        if (message.Length > maxLength)
        {
            return message.Substring(0, maxLength - 3) + "...";
        }
        
        return message;
    }
}
