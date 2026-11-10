import { DeploymentPathEnvironment } from "../models/deploymentpathenvironment.model";
import { Environment } from "../models/environment.model";

export function normalizeDeploymentPathEnvironmentOrder(
  assignments: DeploymentPathEnvironment[] | undefined,
  environments: Environment[] | undefined
): { assignments: DeploymentPathEnvironment[]; unresolvedCount: number } {
  const environmentsById = new Map<number, Environment>();
  for (const environment of environments ?? []) {
    if (typeof environment.Id === "number") {
      environmentsById.set(environment.Id, environment);
    }
  }

  const resolvedAssignments: DeploymentPathEnvironment[] = [];
  for (const assignment of assignments ?? []) {
    const environmentNavigation =
      assignment.EnvironmentNavigation ??
      (typeof assignment.Environment === "number"
        ? environmentsById.get(assignment.Environment)
        : undefined);

    if (typeof assignment.Environment === "number" && environmentNavigation) {
      resolvedAssignments.push({ ...assignment, EnvironmentNavigation: environmentNavigation });
    }
  }

  resolvedAssignments.sort((a, b) => {
    const stepA = Number.isFinite(a.StepNumber)
      ? a.StepNumber
      : Number.MAX_SAFE_INTEGER;
    const stepB = Number.isFinite(b.StepNumber)
      ? b.StepNumber
      : Number.MAX_SAFE_INTEGER;
    return stepA - stepB || (a.Environment ?? 0) - (b.Environment ?? 0);
  });
  const normalizedAssignments = resolvedAssignments.map((assignment, index) => ({
    ...assignment,
    StepNumber: index + 1,
  }));

  return {
    assignments: normalizedAssignments,
    unresolvedCount: (assignments?.length ?? 0) - normalizedAssignments.length,
  };
}
