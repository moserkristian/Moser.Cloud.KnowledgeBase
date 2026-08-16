namespace Moser.Enterprise.Blueprint.BuildingBlocks.Application.CQRS;

public interface ICommand { }

public interface ICommand<out TResult> : ICommand { }
