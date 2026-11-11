using FluentValidation;

namespace LGC.PIT.Plugin.Common.Configs;

public sealed class LGCPluginConfigValidator : AbstractValidator<LGCPluginConfig>
{
    public LGCPluginConfigValidator()
    {
        //// Security Center SDK Credentials
        //RuleFor(x => x.SCLoginUsername)
        //    .NotEmpty().WithMessage("Genetec username is required.")
        //    .Must(NotBeWhiteSpace).WithMessage("Genetec username cannot be whitespace.");

        //RuleFor(x => x.SCLoginPassword)
        //    .NotEmpty().WithMessage("Genetec password is required.")
        //    .Must(NotBeWhiteSpace).WithMessage("Genetec password cannot be whitespace.");

        // LGC API - Service Settings
        RuleFor(x => x.EmployeeUrl)
            .NotEmpty().WithMessage("Employee service URL is required.")
            .Must(NotBeWhiteSpace).WithMessage("Employee service URL cannot be whitespace.");

        RuleFor(x => x.TokenURL)
            .NotEmpty().WithMessage("Token service URL is required.")
            .Must(NotBeWhiteSpace).WithMessage("Token service URL cannot be whitespace.");

        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Client ID is required.")
            .Must(NotBeWhiteSpace).WithMessage("Client ID cannot be whitespace.");

        RuleFor(x => x.ClientSecret)
            .NotEmpty().WithMessage("Client secret is required.")
            .Must(NotBeWhiteSpace).WithMessage("Client secret cannot be whitespace.");
    }

    private static bool NotBeWhiteSpace(string value) =>
      !string.IsNullOrWhiteSpace(value);
}

