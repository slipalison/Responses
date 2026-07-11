namespace Responses;

internal static class ResultMessages
{
    public const string ErrorMessageToSuccess = "There is no error message for success.";

    public const string ErrorMessageIsNotProvidedForFailure = "There must be error message for failure.";

    public const string ValueToFailure = "There is no value for failure.";

    public const string ElseOnVoidResult = "Cannot call Else on Result<void>. Use Result<T> instead.";
}
