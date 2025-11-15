using System;
using System.Linq.Expressions;
using Example.Domain.Common.Abstractions;

namespace Example.Domain.Common.Specifications;

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
