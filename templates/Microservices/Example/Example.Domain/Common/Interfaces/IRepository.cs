using Example.Domain.Common.Abstractions;

using System;

namespace Example.Domain.Common.Interfaces;

public interface IRepository<T> where T : Entity
{
    T GetById(Guid id);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}
