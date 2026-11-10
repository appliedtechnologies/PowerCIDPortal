-- Read-only diagnostic for ApplicationDeploymentPath.HierarchieNumber anomalies.
-- It reports affected applications and assignment rows; it does not modify data.
WITH RankedAssignments AS
(
    SELECT
        assignment.Application AS ApplicationId,
        application.Name AS ApplicationName,
        assignment.DeploymentPath AS DeploymentPathId,
        deploymentPath.Name AS DeploymentPathName,
        deploymentPath.Id AS ResolvedDeploymentPathId,
        assignment.HierarchieNumber,
        COUNT(*) OVER
        (
            PARTITION BY assignment.Application, assignment.HierarchieNumber
        ) AS HierarchyValueCount,
        ROW_NUMBER() OVER
        (
            PARTITION BY assignment.Application
            ORDER BY
                CASE WHEN assignment.HierarchieNumber IS NULL THEN 1 ELSE 0 END,
                assignment.HierarchieNumber,
                assignment.DeploymentPath
        ) AS SequencePosition,
        LAG(assignment.HierarchieNumber) OVER
        (
            PARTITION BY assignment.Application
            ORDER BY
                CASE WHEN assignment.HierarchieNumber IS NULL THEN 1 ELSE 0 END,
                assignment.HierarchieNumber,
                assignment.DeploymentPath
        ) AS PreviousHierarchyNumber,
        MIN(assignment.HierarchieNumber) OVER
        (
            PARTITION BY assignment.Application
        ) AS FirstHierarchyNumber
    FROM dbo.ApplicationDeploymentPath AS assignment
    LEFT JOIN dbo.Application AS application
        ON application.Id = assignment.Application
    LEFT JOIN dbo.Deploymentpath AS deploymentPath
        ON deploymentPath.Id = assignment.DeploymentPath
),
DiagnosedAssignments AS
(
    SELECT
        *,
        CASE WHEN HierarchyValueCount > 1 THEN 1 ELSE 0 END AS IsDuplicateHierarchy,
        CASE WHEN HierarchieNumber IS NULL OR HierarchieNumber <= 0 THEN 1 ELSE 0 END AS IsNullOrNonPositiveHierarchy,
        CASE
            WHEN SequencePosition = 1
                AND (FirstHierarchyNumber IS NULL OR FirstHierarchyNumber <> 1)
            THEN 1 ELSE 0
        END AS DoesNotStartAtOne,
        CASE
            WHEN PreviousHierarchyNumber IS NOT NULL
                AND HierarchieNumber > PreviousHierarchyNumber + 1
            THEN 1 ELSE 0
        END AS HasGapBefore,
        CASE WHEN ResolvedDeploymentPathId IS NULL THEN 1 ELSE 0 END AS IsUnresolvedDeploymentPath
    FROM RankedAssignments
)
SELECT
    ApplicationId,
    ApplicationName,
    SequencePosition,
    DeploymentPathId,
    DeploymentPathName,
    HierarchieNumber,
    IsDuplicateHierarchy,
    IsNullOrNonPositiveHierarchy,
    DoesNotStartAtOne,
    HasGapBefore,
    IsUnresolvedDeploymentPath
FROM DiagnosedAssignments
WHERE IsDuplicateHierarchy = 1
    OR IsNullOrNonPositiveHierarchy = 1
    OR DoesNotStartAtOne = 1
    OR HasGapBefore = 1
    OR IsUnresolvedDeploymentPath = 1
ORDER BY ApplicationId, SequencePosition;
