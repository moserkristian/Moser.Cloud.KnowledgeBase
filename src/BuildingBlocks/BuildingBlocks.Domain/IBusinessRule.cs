namespace Moser.Enterprise.Blueprint.BuildingBlocks.Domain;

public interface IBusinessRule
{
    string Message { get; }
    bool IsBroken();
}
