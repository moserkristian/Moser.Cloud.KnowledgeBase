using System;

namespace Example.Domain.Common.Abstractions;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType()) return false;
        return Id == ((Entity)obj).Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
