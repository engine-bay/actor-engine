namespace EngineBay.ActorEngine
{
    using FluentValidation;

    public class EvaluationDataVariableDtoValidator : AbstractValidator<EvaluationDataVariableDto>
    {
        public EvaluationDataVariableDtoValidator()
        {
            this.RuleFor(evaluationDataVariableDto => evaluationDataVariableDto.Name).NotNull().NotEmpty();
            this.RuleFor(evaluationDataVariableDto => evaluationDataVariableDto.Namespace).NotNull().NotEmpty();
            this.RuleFor(evaluationDataVariableDto => evaluationDataVariableDto.Value).NotNull().NotEmpty();
        }
    }
}