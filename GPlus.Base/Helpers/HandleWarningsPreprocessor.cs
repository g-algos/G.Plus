namespace GPlus.Base.Helpers;

public class HandleWarningsPreprocessor : IFailuresPreprocessor
{
    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        FailureProcessingResult result = FailureProcessingResult.Continue;
        foreach (var failureId in failuresAccessor.GetFailureMessages())
        {
            var severity = failureId.GetSeverity();
            if (severity == FailureSeverity.Warning)
            {
                // Just delete warnings
                failuresAccessor.DeleteWarning(failureId);
            }
            else if (severity == FailureSeverity.Error)
            {
                result = FailureProcessingResult.ProceedWithCommit;
            }
        }

        return result;
    }
}
