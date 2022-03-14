import { Solution } from "./solution.model";

export interface Upgrade extends Solution {
  ApplyManually?: boolean;
}
