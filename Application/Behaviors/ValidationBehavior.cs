using Application.Abstractions.Common;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct = default)
    {
        if (_validators?.Any() != true)
        {
            return await next(ct);
        }

        var context = new ValidationContext<TRequest>(request);
        
        var errors = new List<ValidationFailure>();
        
        //Corro validadores para el TRequest
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, ct);
            errors.AddRange(result.Errors);
        }

        if (errors.Count == 0)
        {
            return await next(ct);
        }
        
        var message = string.Join("; ", errors.Select(e => $"{e.PropertyName} : {e.ErrorMessage}"));
        var error = new Error("ValidationFailed", message);

        return ResultHelper.FailureAs<TResponse>(error);
    }
}
