using System;
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

        // GraphQL uses a single HTTPS endpoint and an email/password login.
        RuleFor(x => x.GraphQLUrl)
            .NotEmpty().WithMessage("GraphQL URL is required.")
            .Must(value => Uri.TryCreate(value, UriKind.Absolute, out var url)
                && url.Scheme == Uri.UriSchemeHttps)
            .WithMessage("GraphQL URL must be an absolute HTTPS URL.");

        RuleFor(x => x.GraphQLEmail)
            .NotEmpty().WithMessage("GraphQL login email is required.")
            .EmailAddress().WithMessage("Enter a valid GraphQL login email.");

        RuleFor(x => x.GraphQLPassword)
            .NotEmpty().WithMessage("GraphQL password is required.")
            .Must(NotBeWhiteSpace).WithMessage("GraphQL password cannot be whitespace.");
    }

    private static bool NotBeWhiteSpace(string value) =>
      !string.IsNullOrWhiteSpace(value);
}

