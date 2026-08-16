namespace VetLims.Infrastructure;

public sealed class OrganizationOwnershipException : InvalidOperationException
{
    public OrganizationOwnershipException(string message) : base(message)
    {
    }
}
