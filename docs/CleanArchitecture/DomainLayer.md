# Domain layer

* **/Aggregates/**: Organizes domain aggregates—each with its root, value objects, domain events, and repository interfaces—representing business consistency boundaries.

* **/Common/**: Contains internal domain contracts and utilities (base types, exceptions, and specifications) used solely within the Domain layer.
    * **/Abstractions/**: Contains the core base classes (like Entity, ValueObject, and AggregateRoot) that define common domain behavior.
        * **Entity.cs** serves as the base for all domain objects, establishing identity.
        * **ValueObject.cs** provides an equality mechanism for immutable, value-based objects.
        * **AggregateRoot.cs** is the primary entity through which all interactions with an aggregate occur, enforcing its invariants, it extends Entity and adds domain event tracking.
        * **Specification.cs**: Defines the abstract base class for encapsulating business rules as expressions, enabling reusable, composable criteria for filtering domain objects.
        * **DomainEvent.cs**: Centralizes shared event behavior (like recording when the event occurred) and ensures consistency across all domain events.
        * **DomainException.cs** is a simple custom exception type for domain rule violations.
    * **/Exceptions/**: Houses all custom domain exception types inheriting from DomainException for consistent error signaling.
    * **/Interfaces/**: Defines shared interfaces (e.g. generic repository contracts) used throughout the domain.
        * **IRepository.cs** is defined as a generic interface for basic CRUD operations.
    * **/Specifications/**: Contains the base Specification class composite implementations for encapsulating business rules.
        * **AndSpecification.cs** implements the logical AND combination of two specifications.
        * **OrSpecification.cs** implements the logical OR combination of two specifications.
        * **NotSpecification.cs** implements the logical negation of a given specification.

* **/Shared/**: Holds cross-layer artifacts that are exposed to and consumed by outer layers.

+ Root
    + src
        + Microservice_1
            + Domain
                + Aggregates
                    + <AggregateRootName>
                        + <AggregateRootName>.cs
                        + <ChildEntityName>.cs
                        + <ValueObjectName>.cs
                        + <EntityName><FactName>DomainEvent.cs
                        + I<AggregateRootName>Repository.cs
                + Common
                    + Abstractions
                        + Entity.cs
                        + ValueObject.cs
                        + AggregateRoot.cs
                        + Specification.cs
                        + DomainEvent.cs
                        + DomainException.cs
                    + Exceptions
                        + <DomainExceptionName>Exception.cs
                    + Interfaces
                        + IRepository.cs
                    + Specifications
                        + AndSpecification.cs
                        + OrSpecification.cs
                        + NotSpecification.cs
                + Shared

## Domain/Common/Abstractions/Entity.cs

```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity()
    {
        Id = Guid.NewGuid();
    }
}
```
## Domain/Common/Abstractions/ValueObject.cs

```csharp
public abstract class ValueObject
{
}
```
## Domain/Common/Abstractions/AggregateRoot.cs

```csharp
public abstract class AggregateRoot : Entity
{
    private readonly List<INotification> _domainEvents = new List<INotification>();
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(INotification eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```
## Domain/Common/Abstractions/Specification.cs

```csharp
public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T candidate)
    {
        return ToExpression().Compile()(entity);
    }

    public Specification<T> And(Specification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    public Specification<T> Or(Specification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}
```
## Domain/Common/Abstractions/DomainEvent.cs

```csharp
public abstract class DomainEvent
{
    public DateTime OccurredOn { get; }

    protected DomainEvent()
    {
        OccurredOn = DateTime.UtcNow;
    }
}
```
## Domain/Common/Abstractions/DomainException.cs

```csharp
public class DomainException : Exception
{
    public DomainException() { }

    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
```
## Domain/Common/Interfaces/IRepository.cs

```csharp
public interface IRepository<T>
    where T : Entity
{
    T GetById(Guid id);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}
```
## Domain/Common/Specifications/AndSpecification.cs

```csharp
public class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExp = _left.ToExpression();
        var rightExp = _right.ToExpression();
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(leftExp, parameter),
            Expression.Invoke(rightExp, parameter)
        );
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
```
## Domain/Common/Specifications/OrSpecification.cs

```csharp
public class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExp = _left.ToExpression();
        var rightExp = _right.ToExpression();
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.OrElse(
            Expression.Invoke(leftExp, parameter),
            Expression.Invoke(rightExp, parameter)
        );
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
```
## Domain/Common/Specifications/NotSpecification.cs

```csharp
public class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _specification;

    public NotSpecification(Specification<T> specification)
    {
        _specification = specification ?? throw new ArgumentNullException(nameof(specification));
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var exp = _specification.ToExpression();
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.Not(Expression.Invoke(exp, parameter));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
```
