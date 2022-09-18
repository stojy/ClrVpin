using System;
using System.Linq.Expressions;

namespace Utils;

// allow class members (aka property and field) to be assigned via a supplied Func (aka getter expression)
// - primarily useful for properties, since these can't be passed by ref into methods.. but works also for fields
// - generates a dynamic setter lambda by..
//   - accepts a Func which is treated as an Expression
//   - break expression into two.. body and parameter
//   - compile and store a setter.. and also compiles a getter for completeness
// - inspired from this post, but simplified to remove unnecessary reflection and field vs member expressions: https://stackoverflow.com/a/43498938/227110
public class Accessor<T>
{
    public Accessor(Expression<Func<T>> expression)
    {
        if (expression.Body is not MemberExpression memberExpression)
            throw new ArgumentException("expression must be return a field or property");
        
        var parameterExpression = Expression.Parameter(typeof(T));

        _setter = Expression.Lambda<Action<T>>(Expression.Assign(memberExpression, parameterExpression), parameterExpression).Compile();
        _getter = expression.Compile();
    }

    public void Set(T value) => _setter(value);
    public T Get() => _getter();

    private readonly Action<T> _setter;
    private readonly Func<T> _getter;
}