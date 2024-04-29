using APBD7.DTOs;
using FluentValidation;

namespace APBD7.Validators;

public class CreateProductWarehouseValidator : AbstractValidator<CreateProductWarehouse>
{
    public CreateProductWarehouseValidator()
    {
        RuleFor(e => e.Amount).GreaterThan(0).NotNull();
        RuleFor(e => e.CreatedAt).NotNull();
        RuleFor(e => e.IdProduct).NotNull();
        RuleFor(e => e.IdWarehouse).NotNull();
    }
    
}