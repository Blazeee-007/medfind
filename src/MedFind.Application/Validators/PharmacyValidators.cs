using FluentValidation;
using MedFind.Application.DTOs;

namespace MedFind.Application.Validators;

public class NearbyQueryValidator : AbstractValidator<NearbyQuery>
{
    public NearbyQueryValidator()
    {
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusKm).GreaterThan(0).LessThanOrEqualTo(25);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(50);
        RuleFor(x => x.Page).GreaterThan(0);
    }
}

public class UpdateStockRequestValidator : AbstractValidator<UpdateStockRequest>
{
    public UpdateStockRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}

public class AddInventoryRequestValidator : AbstractValidator<AddInventoryRequest>
{
    public AddInventoryRequestValidator()
    {
        RuleFor(x => x.MedicineId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}
