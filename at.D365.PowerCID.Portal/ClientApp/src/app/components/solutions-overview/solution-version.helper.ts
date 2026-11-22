import { Solution } from "src/app/shared/models/solution.model";
import { Upgrade } from "src/app/shared/models/upgrade.model";

export function isNewerUpgrade(upgrade: Upgrade, solution: Solution): boolean {
  if (
    upgrade.Application !== solution.Application ||
    upgrade.Id == null ||
    solution.Id == null ||
    !upgrade.CreatedOn ||
    !solution.CreatedOn
  ) {
    return false;
  }

  const upgradeCreatedOn = getDateTimeMilliseconds(upgrade.CreatedOn);
  const solutionCreatedOn = getDateTimeMilliseconds(solution.CreatedOn);
  if (Number.isNaN(upgradeCreatedOn) || Number.isNaN(solutionCreatedOn)) {
    return false;
  }

  if (upgradeCreatedOn !== solutionCreatedOn) {
    return upgradeCreatedOn > solutionCreatedOn;
  }

  return upgrade.Id > solution.Id;
}

function getDateTimeMilliseconds(value: string | Date): number {
  return value instanceof Date ? value.getTime() : Date.parse(value);
}
