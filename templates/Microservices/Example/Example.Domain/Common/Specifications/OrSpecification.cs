using System;
using System.Linq.Expressions;
using Example.Domain.Common.Abstractions;

namespace Example.Domain.Common.Specifications;

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
