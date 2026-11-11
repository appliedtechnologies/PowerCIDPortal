-- Read-only diagnostic for DeploymentPathEnvironment.StepNumber anomalies.
-- It reports affected deployment paths and assignment rows; it does not modify data.
WITH RankedAssignments AS
(
    SELECT
        assignment.DeploymentPath AS DeploymentPathId,
        deploymentPath.Name AS DeploymentPathName,
        assignment.Environment AS EnvironmentId,
        environment.Name AS EnvironmentName,
        environment.Id AS ResolvedEnvironmentId,
        assignment.StepNumber,
        COUNT(*) OVER
        (
            PARTITION BY assignment.DeploymentPath, assignment.StepNumber
        ) AS StepNumberCount,
        ROW_NUMBER() OVER
        (
            PARTITION BY assignment.DeploymentPath
            ORDER BY assignment.StepNumber, assignment.Environment
        ) AS SequencePosition,
        LAG(assignment.StepNumber) OVER
        (
            PARTITION BY assignment.DeploymentPath
            ORDER BY assignment.StepNumber, assignment.Environment
        ) AS PreviousStepNumber,
        MIN(assignment.StepNumber) OVER
        (
            PARTITION BY assignment.DeploymentPath
        ) AS FirstStepNumber,
        deploymentPath.Id AS ResolvedDeploymentPathId
    FROM dbo.DeploymentPathEnvironment AS assignment
    LEFT JOIN dbo.Deploymentpath AS deploymentPath
        ON deploymentPath.Id = assignment.DeploymentPath
    LEFT JOIN dbo.Environment AS environment
        ON environment.Id = assignment.Environment
),
DiagnosedAssignments AS
(
    SELECT
        *,
        CASE WHEN StepNumberCount > 1 THEN 1 ELSE 0 END AS IsDuplicateStepNumber,
        CASE WHEN StepNumber <= 0 THEN 1 ELSE 0 END AS IsNonPositiveStepNumber,
        CASE
            WHEN SequencePosition = 1 AND FirstStepNumber <> 1
            THEN 1 ELSE 0
        END AS DoesNotStartAtOne,
        CASE
            WHEN PreviousStepNumber IS NOT NULL
                AND StepNumber > PreviousStepNumber + 1
            THEN 1 ELSE 0
        END AS HasGapBefore,
        CASE WHEN ResolvedEnvironmentId IS NULL THEN 1 ELSE 0 END AS IsUnresolvedEnvironment,
        CASE WHEN ResolvedDeploymentPathId IS NULL THEN 1 ELSE 0 END AS IsUnresolvedDeploymentPath
    FROM RankedAssignments
)
SELECT
    DeploymentPathId,
    DeploymentPathName,
    SequencePosition,
    EnvironmentId,
    EnvironmentName,
    StepNumber,
    IsDuplicateStepNumber,
    IsNonPositiveStepNumber,
    DoesNotStartAtOne,
    HasGapBefore,
    IsUnresolvedEnvironment,
    IsUnresolvedDeploymentPath
FROM DiagnosedAssignments
WHERE IsDuplicateStepNumber = 1
    OR IsNonPositiveStepNumber = 1
    OR DoesNotStartAtOne = 1
    OR HasGapBefore = 1
    OR IsUnresolvedEnvironment = 1
    OR IsUnresolvedDeploymentPath = 1
ORDER BY DeploymentPathId, SequencePosition;
