namespace VirtualWebDisplay.Web.Handlers;

internal static class TouchInputRequestValidator
{
    internal const string MissingBodyError = "Request body required";
    internal const string MissingTypeError = "Type field required";

    internal static bool TryValidate(TouchInputRequest? request, out string errorMessage)
    {
        if (request is null)
        {
            errorMessage = MissingBodyError;
            return false;
        }

        if (string.IsNullOrEmpty(request.Type))
        {
            errorMessage = MissingTypeError;
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
