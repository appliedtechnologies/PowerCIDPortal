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

  const upgradeCreatedOn = Date.parse(upgrade.CreatedOn);
  const solutionCreatedOn = Date.parse(solution.CreatedOn);
  if (Number.isNaN(upgradeCreatedOn) || Number.isNaN(solutionCreatedOn)) {
    return false;
  }

  if (upgradeCreatedOn !== solutionCreatedOn) {
    return upgradeCreatedOn > solutionCreatedOn;
  }

  const upgradeSubMillisecondTicks = getSubMillisecondTicks(upgrade.CreatedOn);
  const solutionSubMillisecondTicks = getSubMillisecondTicks(solution.CreatedOn);
  if (upgradeSubMillisecondTicks !== solutionSubMillisecondTicks) {
    return upgradeSubMillisecondTicks > solutionSubMillisecondTicks;
  }

  return upgrade.Id > solution.Id;
}

function getSubMillisecondTicks(value: string): number {
  const fractionalSeconds = value.match(/T\d{2}:\d{2}:\d{2}\.(\d+)/)?.[1] ?? "";
  return Number(fractionalSeconds.padEnd(7, "0").slice(3, 7));
}
