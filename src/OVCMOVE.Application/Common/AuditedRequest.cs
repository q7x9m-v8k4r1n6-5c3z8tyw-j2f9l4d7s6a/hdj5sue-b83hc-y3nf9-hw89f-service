namespace OVCMOVE.Application.Common;

/// <summary>Marks a command whose actor is supplied by the request pipeline.</summary>
public abstract class AuditedRequest
{
    public string? Actor { get; internal set; }

    /// <summary>Returns the normalized actor or the non-HTTP fallback.</summary>
    public string GetActorOrSystem() =>
        string.IsNullOrWhiteSpace(Actor) ? "system" : Actor.Trim();
}
