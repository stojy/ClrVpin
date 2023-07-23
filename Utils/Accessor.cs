using System;
using System.Linq.Expressions;

namespace Utils;

// allow class members (aka property and field) to be assigned via a supplied Func
// - primarily useful for properties, since these can't be passed by ref into methods.. but works also for fields
// - refer
//   - https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/
//   - https://stackoverflow.com/a/43498938/227110.. modified to remove unnecessary reflection and member handling
public class Accessor<T>
{
    // accept lambda expression and allow compiler to emit the expression tree
    public Accessor(Expression<Func<T>> expression)
    {
        // decompose expression into member (property or field) and parameter (for assignment)
        if (!IsSupported(expression, out var memberExpression))
            throw new ArgumentException("expression must be return a field or property");
        var parameterExpression = Expression.Parameter(typeof(T));

        // create a new expression to support setter
        _setter = Expression.Lambda<Action<T>>(Expression.Assign(memberExpression, parameterExpression), parameterExpression).Compile();

        // re-use the compiler expression to support getter
        _getter = expression.Compile();

        // store the member name
        Name = GetName(memberExpression);
    }

    public string Name { get; }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public static bool TryGetName(Expression<Func<T>> expression, out string name)
    {
        if (IsSupported(expression, out var memberExpression))
        {
            name = GetName(memberExpression);
            return true;
        }

        name = null;
        return false;
    }

    public void Set(T value) => _setter(value);
    public T Get() => _getter();

    private static string GetName(MemberExpression memberExpression) => memberExpression.Member.Name;

    private static bool IsSupported(Expression<Func<T>> expression, out MemberExpression memberExpression)
    {
        if (expression.Body is MemberExpression privateMemberExpression)
        {
            memberExpression = privateMemberExpression;
            return true;
        }

        memberExpression = null;
        return false;
    }

    private readonly Action<T> _setter;
    private readonly Func<T> _getter;
}