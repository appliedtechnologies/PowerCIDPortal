import { Solution } from "./solution.model";

export interface Patch extends Solution {
    WasDeleted?: boolean;
}