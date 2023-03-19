namespace EngineBay.ActorEngine
{
    using FluentValidation;

    public class CreateEvaluationDtoValidator : AbstractValidator<CreateEvaluationDto>
    {
        public CreateEvaluationDtoValidator()
        {
            this.RuleFor(createEvaluationDto => createEvaluationDto.WorkbookId).NotNull().NotEmpty();
            this.RuleFor(createEvaluationDto => createEvaluationDto.DataVariables).NotNull();
            this.RuleForEach(createEvaluationDto => createEvaluationDto.DataVariables).SetValidator(new EvaluationDataVariableDtoValidator());
        }
    }
}